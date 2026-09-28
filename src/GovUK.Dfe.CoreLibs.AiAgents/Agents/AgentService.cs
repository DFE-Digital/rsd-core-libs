using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.Diagnostics;
using GovUK.Dfe.CoreLibs.AiAgents.Extensions;
using GovUK.Dfe.CoreLibs.AiAgents.Tools;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using GovUK.Dfe.CoreLibs.AiAgents.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

internal sealed class AgentService(IAgentRunner agentRunner, IAgentRuntime agentRuntime, AgentSpecBuilder specs,
    AgentRunOptions? runOptions = null, Factories.Interfaces.IAgentFactory? agentFactory = null, ILogger<AgentService>? logger = null)
    : IAgentService
{
    private readonly AgentSpecBuilder _specs = specs;
    private readonly ILogger<AgentService> _logger = logger ?? NullLogger<AgentService>.Instance;
    private readonly string _applicationName = runOptions?.ApplicationName ?? AgentTelemetry.DefaultApplicationName;

    public Task<AgentResult> RunAsync(AgentDefinition definition, string prompt, string? evidence = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        return RunAgentAsync(definition, prompt, evidence, cancellationToken);
    }

    public async Task<IReadOnlyList<AgentResult>> RunParallelAsync(IReadOnlyCollection<AgentDefinition> definitions,
        Func<AgentDefinition, CancellationToken, Task<string>> resolvePrompt, AgentContext context,
        Func<Exception, bool>? shouldSuppress = null, CancellationToken cancellationToken = default,
        Func<AgentDefinition, CancellationToken, Task<string?>>? resolveEvidence = null)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(resolvePrompt);
        ArgumentNullException.ThrowIfNull(context);

        using var activity = AgentTelemetry.StartOrchestration(_applicationName, AgentTelemetry.ParallelMode, definitions.Count);

        // Concurrency limits apply in the runner, to every run on the instance (and globally when configured).
        // A failure that isn't suppressed cancels the other agents straight away rather than letting
        // them run - and bill - to completion for a result that will be thrown away.
        var steps = await FailFastParallel.WhenAllAsync(definitions.Select(definition =>
                (Func<CancellationToken, Task<AgentStepResult>>)(token =>
                    RunStepAsync(definition, async ct =>
                        (await resolvePrompt(definition, ct).ConfigureAwait(false),
                         resolveEvidence is null ? null : await resolveEvidence(definition, ct).ConfigureAwait(false)),
                        shouldSuppress, token))),
                cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<AgentResult> results = [.. steps.Select(step => step.ToAgentResult())];
        AgentTelemetry.RecordOrchestration(activity, _applicationName, AgentTelemetry.ParallelMode,
            results.ToTokenUsageSummary().Total, failedAgents: steps.Count(step => !step.Succeeded));
        return results;
    }

    public async Task<IReadOnlyList<AgentResult>> RunSequentialAsync(IReadOnlyList<AgentDefinition> definitions,
        Func<AgentDefinition, CancellationToken, Task<string>> resolvePrompt, string initialInput,
        AgentContext context, Func<Exception, bool>? shouldSuppress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(resolvePrompt);
        ArgumentNullException.ThrowIfNull(context);

        using var activity = AgentTelemetry.StartOrchestration(_applicationName, AgentTelemetry.SequentialMode, definitions.Count);

        var results = new List<AgentResult>(definitions.Count);
        var previousOutput = initialInput;
        var failedAgents = 0;

        foreach (var definition in definitions)
        {
            string? prompt = null;
            var input = previousOutput;

            // The previous output goes in as fenced evidence, never as part of the prompt: whatever
            // influenced it (e.g. injected text in search results) can't become an instruction here.
            var step = await RunStepAsync(definition,
                async ct =>
                {
                    prompt = await resolvePrompt(definition, ct).ConfigureAwait(false);
                    return (prompt, input);
                },
                shouldSuppress, cancellationToken).ConfigureAwait(false);

            var result = step.ToAgentResult();
            results.Add(result);

            if (step.Succeeded)
            {
                context.AddHistory(new AgentContextEntry(definition.Name, prompt ?? string.Empty, result.Output));
                previousOutput = result.Output ?? previousOutput;
            }
            else
            {
                failedAgents++;
            }
        }

        AgentTelemetry.RecordOrchestration(activity, _applicationName, AgentTelemetry.SequentialMode,
            results.ToTokenUsageSummary().Total, failedAgents);
        return results;
    }

    public async Task<IReadOnlyList<AgentReference>> ProvisionAsync(IReadOnlyCollection<AgentDefinition> definitions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var factory = agentFactory ?? throw new InvalidOperationException(Constants.ErrorMessages.ProvisioningNeedsFactory);

        var provisioned = new List<AgentReference>(definitions.Count);

        // One at a time: a provisioning job isn't latency-sensitive, and it keeps Foundry calls gentle.
        foreach (var definition in definitions)
        {
            if (!definition.IsManagedAgent)
            {
                _logger.LogInformation("Skipping {AgentName}: ephemeral agents are created by the app that runs them", definition.Name);
                continue;
            }

            var spec = await _specs.BuildAsync(definition, cancellationToken).ConfigureAwait(false);
            if (spec is null)
            {
                _logger.LogInformation("Skipping {AgentName}: it's managed outside this app", definition.Name);
                continue;
            }

            var agent = await factory.GetOrCreateAsync(spec, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Provisioned {AgentName} at version {Version}", agent.Name, agent.Version);
            provisioned.Add(agent);
        }

        return provisioned;
    }

    /// <summary>
    /// Runs one agent - managed or ephemeral - and records a failure as a step result rather than
    /// throwing, unless <paramref name="shouldSuppress"/> says otherwise.
    /// </summary>
    private Task<AgentStepResult> RunStepAsync(AgentDefinition definition,
        Func<CancellationToken, Task<(string Prompt, string? Evidence)>> resolveInput,
        Func<Exception, bool>? shouldSuppress, CancellationToken cancellationToken)
        => ResilientAgentStep.ExecuteAsync(
            step: async () =>
            {
                var (prompt, evidence) = await resolveInput(cancellationToken).ConfigureAwait(false);
                var result = await RunAgentAsync(definition, prompt, evidence, cancellationToken).ConfigureAwait(false);
                return new AgentStepResult(definition.Name, result, Error: null);
            },
            fallback: ex =>
            {
                _logger.LogError(ex, "Agent {AgentName} failed; recording failure and continuing", definition.Name);
                return new AgentStepResult(definition.Name, Result: null, ex);
            },
            shouldSuppress: shouldSuppress);

    private async Task<AgentResult> RunAgentAsync(AgentDefinition definition, string prompt, string? evidence,
        CancellationToken cancellationToken)
    {
        // Tools bound to this agent that run in this app (e.g. MCP) execute the model's calls here,
        // limited to the definition's AllowedTools.
        var resolveToolCalls = AgentToolResolver.CreateToolCallResolver(_specs.ToolProviders, definition);

        if (!definition.IsManagedAgent)
        {
            var spec = await _specs.BuildAsync(definition, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException(string.Format(Constants.ErrorMessages.EphemeralAgentNeedsSpec, definition.Name));
            return await agentRuntime.RunEphemeralAsync(spec, prompt, resolveToolCalls, evidence, cancellationToken).ConfigureAwait(false);
        }

        var agent = _specs.IsExternallyManaged(definition)
            ? await _specs.CustomProviderFor(definition)!.GetAgentAsync(cancellationToken).ConfigureAwait(false)
            : await agentRuntime.GetOrCreateAsync(definition.Name,
                async ct => (await _specs.BuildAsync(definition, ct).ConfigureAwait(false))!, cancellationToken).ConfigureAwait(false);

        return await agentRunner.RunAsync(agent, prompt, additionalContext: evidence, resolveToolCalls: resolveToolCalls,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
