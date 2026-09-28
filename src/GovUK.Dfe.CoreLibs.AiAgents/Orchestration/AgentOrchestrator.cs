using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.Diagnostics;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Resilience;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Orchestration;

public sealed class AgentOrchestrator(IAgentRunner agentRunner, ILogger<AgentOrchestrator>? logger = null,
    AgentRunOptions? runOptions = null) : IAgentOrchestrator
{
    private readonly ILogger<AgentOrchestrator> _logger = logger ?? NullLogger<AgentOrchestrator>.Instance;
    private readonly string _applicationName = runOptions?.ApplicationName ?? AgentTelemetry.DefaultApplicationName;

    public async Task<OrchestrationResult> RunSequentialAsync(IReadOnlyList<AgentReference> agents, string initialInput,
        AgentContext context, Func<Exception, bool>? shouldSuppress = null, CancellationToken cancellationToken = default,
        Func<AgentReference, ToolCallResolver?>? resolveToolCallsFor = null)
    {
        ArgumentNullException.ThrowIfNull(agents);
        ArgumentNullException.ThrowIfNull(context);

        using var activity = AgentTelemetry.StartOrchestration(_applicationName, AgentTelemetry.SequentialMode, agents.Count);

        var results = new List<AgentStepResult>(agents.Count);
        string? lastOutput = null;

        foreach (var agent in agents)
        {
            // Every agent gets the caller's input as its prompt. What earlier agents produced goes in as
            // fenced evidence - never spliced into the prompt - so text that influenced one agent (e.g.
            // injected into search results) can't become an instruction to the next.
            var step = new AgentOrchestrationStep(agent.Name, _ => Task.FromResult(agent), _ => Task.FromResult(initialInput))
            {
                ResolveEvidence = _ => Task.FromResult(NullIfEmpty(context.BuildContextPrompt())),
                ResolveToolCalls = resolveToolCallsFor?.Invoke(agent),
            };

            var result = await RunStepAsync(step, context, recordHistory: true, shouldSuppress, cancellationToken).ConfigureAwait(false);
            results.Add(result);

            if (result.Succeeded)
            {
                lastOutput = result.Result!.Output;
            }
        }

        var orchestration = new OrchestrationResult(lastOutput, results);
        AgentTelemetry.RecordOrchestration(activity, _applicationName, AgentTelemetry.SequentialMode, orchestration.Usage,
            results.Count(r => !r.Succeeded));
        return orchestration;
    }

    public Task<OrchestrationResult> RunParallelAsync(IReadOnlyList<AgentReference> agents, string input,
        AgentContext context, int? maxConcurrency = null, Func<Exception, bool>? shouldSuppress = null,
        CancellationToken cancellationToken = default,
        Func<AgentReference, ToolCallResolver?>? resolveToolCallsFor = null)
    {
        ArgumentNullException.ThrowIfNull(agents);

        var steps = agents.Select(agent => new AgentOrchestrationStep(agent.Name, _ => Task.FromResult(agent), _ => Task.FromResult(input))
        {
            ResolveToolCalls = resolveToolCallsFor?.Invoke(agent),
        }).ToList();
        return RunParallelAsync(steps, context, maxConcurrency, shouldSuppress, cancellationToken);
    }

    public async Task<OrchestrationResult> RunParallelAsync(IReadOnlyList<AgentOrchestrationStep> steps,
        AgentContext context, int? maxConcurrency = null, Func<Exception, bool>? shouldSuppress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(context);

        using var activity = AgentTelemetry.StartOrchestration(_applicationName, AgentTelemetry.ParallelMode, steps.Count);
        using var semaphore = maxConcurrency is int max ? new SemaphoreSlim(max, max) : null;

        // A failure that isn't suppressed cancels the other steps rather than letting them run to completion.
        var results = await FailFastParallel.WhenAllAsync(steps.Select(step =>
                (Func<CancellationToken, Task<AgentStepResult>>)(token => RunBoundedStepAsync(step, context, semaphore, shouldSuppress, token))),
                cancellationToken)
            .ConfigureAwait(false);

        var finalOutput = string.Join(Environment.NewLine + Environment.NewLine,
            results.Where(r => r.Succeeded).Select(r => r.Result!.Output));

        var orchestration = new OrchestrationResult(finalOutput, results);
        AgentTelemetry.RecordOrchestration(activity, _applicationName, AgentTelemetry.ParallelMode, orchestration.Usage,
            results.Count(r => !r.Succeeded));
        return orchestration;
    }

    private async Task<AgentStepResult> RunBoundedStepAsync(AgentOrchestrationStep step, AgentContext context,
        SemaphoreSlim? semaphore, Func<Exception, bool>? shouldSuppress, CancellationToken cancellationToken)
    {
        if (semaphore is not null)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            return await RunStepAsync(step, context, recordHistory: false, shouldSuppress, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            semaphore?.Release();
        }
    }

    private async Task<AgentStepResult> RunStepAsync(AgentOrchestrationStep step, AgentContext context,
        bool recordHistory, Func<Exception, bool>? shouldSuppress, CancellationToken cancellationToken)
        => await ResilientAgentStep.ExecuteAsync(
            step: async () =>
            {
                var agent = await step.ResolveAgent(cancellationToken).ConfigureAwait(false);
                var prompt = await step.ResolvePrompt(cancellationToken).ConfigureAwait(false);
                var evidence = step.ResolveEvidence is null ? null : await step.ResolveEvidence(cancellationToken).ConfigureAwait(false);

                var result = await agentRunner.RunAsync(agent, prompt, additionalContext: evidence,
                    resolveToolCalls: step.ResolveToolCalls, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (recordHistory)
                {
                    context.AddHistory(new AgentContextEntry(agent.Name, prompt, result.Output));
                }

                return new AgentStepResult(agent.Name, result, Error: null);
            },
            fallback: ex =>
            {
                _logger.LogError(ex, "Agent step {AgentName} failed; recording failure and continuing", step.AgentName);
                return new AgentStepResult(step.AgentName, Result: null, ex);
            },
            shouldSuppress: shouldSuppress).ConfigureAwait(false);

    private static string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;
}
