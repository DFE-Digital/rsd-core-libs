using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration.Inerfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Resilience;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Orchestration;
 
public sealed class AgentOrchestrator(IAgentRunner agentRunner, ILogger<AgentOrchestrator>? logger = null) : IAgentOrchestrator
{
    private readonly ILogger<AgentOrchestrator> _logger = logger ?? NullLogger<AgentOrchestrator>.Instance;
    
    public async Task<OrchestrationResult> RunSequentialAsync(IReadOnlyList<AgentReference> agents, string initialInput,
        AgentContext context, Func<Exception, bool>? shouldSuppress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agents);
        ArgumentNullException.ThrowIfNull(context);

        var results = new List<AgentStepResult>(agents.Count);
        var currentInput = initialInput;
        string? lastOutput = null;

        foreach (var agent in agents)
        {
            var input = currentInput;
            var step = new AgentOrchestrationStep(agent.Name, _ => Task.FromResult(agent), _ => Task.FromResult(input));
            var result = await RunStepAsync(step, context, true, shouldSuppress, cancellationToken);
            results.Add(result);

            if (result.Succeeded)
            {
                lastOutput = result.Result!.Output;
                currentInput = lastOutput ?? currentInput;
            }
        }

        return new OrchestrationResult(lastOutput, results);
    }
     
    public Task<OrchestrationResult> RunParallelAsync(IReadOnlyList<AgentReference> agents, string input,
        AgentContext context, int? maxConcurrency = null, Func<Exception, bool>? shouldSuppress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agents);

        var steps = agents.Select(agent => new AgentOrchestrationStep(agent.Name, _ => Task.FromResult(agent), _ => Task.FromResult(input))).ToList();
        return RunParallelAsync(steps, context, maxConcurrency, shouldSuppress, cancellationToken);
    }
     
    public async Task<OrchestrationResult> RunParallelAsync(IReadOnlyList<AgentOrchestrationStep> steps,
        AgentContext context, int? maxConcurrency = null, Func<Exception, bool>? shouldSuppress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(context);

        using var semaphore = maxConcurrency is int max ? new SemaphoreSlim(max, max) : null;

        var tasks = steps.Select(step => RunBoundedStepAsync(step, context, semaphore, shouldSuppress, cancellationToken));
        var results = await Task.WhenAll(tasks);

        var finalOutput = string.Join(Environment.NewLine + Environment.NewLine,
            results.Where(r => r.Succeeded).Select(r => r.Result!.Output));

        return new OrchestrationResult(finalOutput, results);
    }

    /// <summary>
    /// Runs a single step with optional concurrency control.
    /// </summary>
    /// <param name="step">The step to run.</param>
    /// <param name="context">The context for the agent step.</param>
    /// <param name="semaphore">The semaphore for concurrency control.</param>
    /// <param name="shouldSuppress">A function to determine whether to suppress exceptions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the agent step.</returns>
    private async Task<AgentStepResult> RunBoundedStepAsync(AgentOrchestrationStep step, AgentContext context,
        SemaphoreSlim? semaphore, Func<Exception, bool>? shouldSuppress, CancellationToken cancellationToken)
    {
        if (semaphore is not null)
        {
            await semaphore.WaitAsync(cancellationToken);
        }

        try
        {
            return await RunStepAsync(step, context, false, shouldSuppress, cancellationToken);
        }
        finally
        {
            semaphore?.Release();
        }
    }

    /// <summary>
    /// Runs a single agent orchestration step, handling prompt building, context management, and error handling.
    /// </summary>
    /// <param name="step">The step to run.</param>
    /// <param name="context">The context for the agent step.</param>
    /// <param name="prependContext">A value indicating whether to prepend the context to the prompt.</param>
    /// <param name="shouldSuppress">A function to determine whether to suppress exceptions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the agent step.</returns>
    private async Task<AgentStepResult> RunStepAsync(AgentOrchestrationStep step, AgentContext context,
        bool prependContext, Func<Exception, bool>? shouldSuppress, CancellationToken cancellationToken)
        => await ResilientAgentStep.ExecuteAsync(
            step: async () =>
            {
                var agent = await step.ResolveAgent(cancellationToken);
                var rawPrompt = await step.ResolvePrompt(cancellationToken);
                var prompt = prependContext ? BuildPrompt(rawPrompt, context) : rawPrompt;
                var result = await agentRunner.RunAsync(agent, prompt, cancellationToken: cancellationToken);

                if (prependContext)
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
            shouldSuppress: shouldSuppress);

    private static string BuildPrompt(string input, AgentContext context)
    {
        var contextPrompt = context.BuildContextPrompt();
        return string.IsNullOrEmpty(contextPrompt) ? input : $"{contextPrompt}{Environment.NewLine}{Environment.NewLine}{input}";
    }
}
