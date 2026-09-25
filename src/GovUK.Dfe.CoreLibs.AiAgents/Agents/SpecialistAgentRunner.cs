using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.Extensions;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Tools;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

public sealed class SpecialistAgentRunner(IAgentFactory agentFactory, IAgentRunner agentRunner, IAgentRuntime agentRuntime,
    IPromptProvider promptProvider, IEnumerable<IManagedAgentProvider>? managedAgentProviders = null,
    IEnumerable<AgentToolBinding>? toolBindings = null, ILogger<SpecialistAgentRunner>? logger = null) : ISpecialistAgentRunner
{
    private readonly Dictionary<string, IManagedAgentProvider> _managedAgents =
        (managedAgentProviders ?? []).ToDictionary(static provider => provider.AgentName);
    private readonly IReadOnlyDictionary<string, List<IAgentToolProvider>> _toolProviders = AgentToolResolver.GroupByAgentName(toolBindings);
    private readonly ILogger<SpecialistAgentRunner> _logger = logger ?? NullLogger<SpecialistAgentRunner>.Instance;
     
    public async Task<IReadOnlyList<AgentResult>> RunParallelAsync(IReadOnlyCollection<AgentDefinition> definitions,
        Func<AgentDefinition, CancellationToken, Task<string>> resolvePrompt, AgentContext context,
        Func<Exception, bool>? shouldSuppress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(resolvePrompt);
        ArgumentNullException.ThrowIfNull(context);

        var managed = definitions.Where(definition => definition.IsManagedAgent).ToList();
        var ephemeral = definitions.Where(definition => !definition.IsManagedAgent).ToList();
         
        var managedTask = managed.Count == 0
            ? Task.FromResult(new OrchestrationResult(null, []))
            : agentRuntime.Orchestrator.RunParallelAsync(
                [.. managed.Select(definition => BuildStep(definition, resolvePrompt, cancellationToken))],
                context, shouldSuppress: shouldSuppress, cancellationToken: cancellationToken);

        var ephemeralTask = Task.WhenAll(
            ephemeral.Select(definition => RunEphemeralAsync(definition, resolvePrompt, shouldSuppress, cancellationToken)));

        await Task.WhenAll(managedTask, ephemeralTask).ConfigureAwait(false);

        return [.. managedTask.Result.Results.Concat(ephemeralTask.Result).Select(step => step.ToAgentResult())];
    }

    public async Task<IReadOnlyList<AgentResult>> RunSequentialAsync(IReadOnlyList<AgentDefinition> definitions,
        Func<AgentDefinition, string, CancellationToken, Task<string>> resolvePrompt, string initialInput,
        AgentContext context, Func<Exception, bool>? shouldSuppress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(resolvePrompt);
        ArgumentNullException.ThrowIfNull(context);

        var results = new List<AgentResult>(definitions.Count);
        var previousOutput = initialInput;

        foreach (var definition in definitions)
        {
            var step = await RunSequentialStepAsync(definition, resolvePrompt, previousOutput, shouldSuppress, cancellationToken)
                .ConfigureAwait(false);
            var result = step.ToAgentResult();
            results.Add(result);

            if (step.Succeeded)
            {
                previousOutput = result.Output ?? previousOutput;
            }
        }

        return results;
    }

    /// <summary>
    /// Runs a single step of a sequential agent orchestration, either as a managed agent or an ephemeral agent, depending on the definition.
    /// </summary>
    /// <param name="definition">The agent definition for the step.</param>
    /// <param name="resolvePrompt">A function to resolve the prompt for the step.</param>
    /// <param name="previousOutput">The output from the previous step.</param>
    /// <param name="shouldSuppress">A function to determine whether to suppress exceptions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation and returning the result of the step.</returns>
    private Task<AgentStepResult> RunSequentialStepAsync(AgentDefinition definition,
        Func<AgentDefinition, string, CancellationToken, Task<string>> resolvePrompt, string previousOutput,
        Func<Exception, bool>? shouldSuppress, CancellationToken cancellationToken)
        => ResilientAgentStep.ExecuteAsync(
            step: () => RunSequentialStepCoreAsync(definition, resolvePrompt, previousOutput, cancellationToken),
            fallback: ex =>
            {
                _logger.LogError(ex, "Specialist agent step {AgentName} failed; recording failure and continuing", definition.Name);
                return new AgentStepResult(definition.Name, Result: null, ex);
            },
            shouldSuppress: shouldSuppress);

    /// <summary>
    /// Runs a single step of a sequential agent orchestration, either as a managed agent or an ephemeral agent, depending on the definition.
    /// </summary>
    /// <param name="definition">The agent definition for the step.</param>
    /// <param name="resolvePrompt">A function to resolve the prompt for the step.</param>
    /// <param name="previousOutput">The output from the previous step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation and returning the result of the step.</returns>
    private async Task<AgentStepResult> RunSequentialStepCoreAsync(AgentDefinition definition,
        Func<AgentDefinition, string, CancellationToken, Task<string>> resolvePrompt, string previousOutput,
        CancellationToken cancellationToken)
    {
        var prompt = await resolvePrompt(definition, previousOutput, cancellationToken).ConfigureAwait(false);

        if (!definition.IsManagedAgent)
        {
            var spec = await BuildEphemeralSpecAsync(definition, cancellationToken).ConfigureAwait(false);
            var ephemeralResult = await agentRuntime.RunEphemeralAsync(spec, prompt, cancellationToken).ConfigureAwait(false);
            return new AgentStepResult(definition.Name, ephemeralResult, Error: null);
        }

        var agent = await ResolveManagedAgentAsync(definition, cancellationToken).ConfigureAwait(false);
        var managedResult = await agentRunner.RunAsync(agent, prompt, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new AgentStepResult(definition.Name, managedResult, Error: null);
    }

    private AgentOrchestrationStep BuildStep(AgentDefinition definition,
        Func<AgentDefinition, CancellationToken, Task<string>> resolvePrompt, CancellationToken cancellationToken)
    {
        Task<AgentReference> resolveAgent(CancellationToken ct) => ResolveManagedAgentAsync(definition, cancellationToken);
        Task<string> resolvePromptForStep(CancellationToken ct) => resolvePrompt(definition, cancellationToken);

        return new AgentOrchestrationStep(definition.Name, resolveAgent, resolvePromptForStep);
    }

    private Task<AgentReference> ResolveManagedAgentAsync(AgentDefinition definition, CancellationToken cancellationToken)
    {
        if (_managedAgents.TryGetValue(definition.Name, out var provider))
        {
            return provider.GetAgentAsync(cancellationToken);
        }

        return ResolveDefaultManagedAgentAsync(definition, cancellationToken);
    }

    /// <summary>
    /// Resolves a default managed agent for the given definition by creating or retrieving an agent based on the definition's name and system prompt type.
    /// </summary>
    /// <param name="definition"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<AgentReference> ResolveDefaultManagedAgentAsync(AgentDefinition definition, CancellationToken cancellationToken)
    {
        var spec = new AgentSpec
        {
            Name = definition.Name,
            Instructions = promptProvider.GetSystemPrompt(definition.SystemPromptType),
            Tools = await AgentToolResolver.ResolveAsync(_toolProviders, definition.Name, cancellationToken).ConfigureAwait(false),
        };

        var created = await agentFactory.GetOrCreateAsync(spec, cancellationToken).ConfigureAwait(false);
        return await agentRuntime.ResolveAsync(created, cancellationToken).ConfigureAwait(false);
    }

    private Task<AgentStepResult> RunEphemeralAsync(AgentDefinition definition,
        Func<AgentDefinition, CancellationToken, Task<string>> resolvePrompt, Func<Exception, bool>? shouldSuppress,
        CancellationToken cancellationToken)
        => ResilientAgentStep.ExecuteAsync(
            step: () => RunEphemeralCoreAsync(definition, resolvePrompt, cancellationToken),
            fallback: ex =>
            {
                _logger.LogError(ex, "Specialist agent step {AgentName} failed; recording failure and continuing", definition.Name);
                return new AgentStepResult(definition.Name, Result: null, ex);
            },
            shouldSuppress: shouldSuppress);

    private async Task<AgentStepResult> RunEphemeralCoreAsync(AgentDefinition definition,
        Func<AgentDefinition, CancellationToken, Task<string>> resolvePrompt, CancellationToken cancellationToken)
    {
        var prompt = await resolvePrompt(definition, cancellationToken).ConfigureAwait(false);
        var spec = await BuildEphemeralSpecAsync(definition, cancellationToken).ConfigureAwait(false);
        var result = await agentRuntime.RunEphemeralAsync(spec, prompt, cancellationToken).ConfigureAwait(false);
        return new AgentStepResult(definition.Name, result, Error: null);
    }

    private async Task<AgentSpec> BuildEphemeralSpecAsync(AgentDefinition definition, CancellationToken cancellationToken) => new()
    {
        Name = definition.Name,
        Instructions = promptProvider.GetSystemPrompt(definition.SystemPromptType),
        Tools = await AgentToolResolver.ResolveAsync(_toolProviders, definition.Name, cancellationToken).ConfigureAwait(false),
    };
}
