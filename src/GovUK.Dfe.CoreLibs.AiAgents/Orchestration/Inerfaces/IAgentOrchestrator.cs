using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Orchestration.Inerfaces;

/// <summary>
/// Defines methods for orchestrating the execution of agents, either sequentially or concurrently.
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Runs agents sequentially, passing each successful output to the next.
    /// </summary>
    /// <param name="agents">The already-resolved agents to run.</param>
    /// <param name="initialInput">The initial input for the first agent.</param>
    /// <param name="context">The context for the agents.</param>
    /// <param name="shouldSuppress">Determines which exceptions to suppress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the orchestration.</returns>
    Task<OrchestrationResult> RunSequentialAsync(IReadOnlyList<AgentReference> agents, string initialInput,
        AgentContext context, Func<Exception, bool>? shouldSuppress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs agents concurrently using the same input.
    /// </summary>
    /// <param name="agents">The already-resolved agents to run.</param>
    /// <param name="input">The input for each agent.</param>
    /// <param name="context">The agent context.</param>
    /// <param name="maxConcurrency">The maximum number of concurrent agents.</param>
    /// <param name="shouldSuppress">Determines which exceptions to suppress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The orchestration result.</returns>
    Task<OrchestrationResult> RunParallelAsync(IReadOnlyList<AgentReference> agents, string input,
        AgentContext context, int? maxConcurrency = null, Func<Exception, bool>? shouldSuppress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs orchestration steps concurrently, resolving each agent and prompt independently.
    /// </summary>
    /// <param name="steps">The orchestration steps to run.</param>
    /// <param name="context">The agent context.</param>
    /// <param name="maxConcurrency">The maximum number of concurrent steps.</param>
    /// <param name="shouldSuppress">Determines which exceptions to suppress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The orchestration result.</returns>
    Task<OrchestrationResult> RunParallelAsync(IReadOnlyList<AgentOrchestrationStep> steps,
        AgentContext context, int? maxConcurrency = null, Func<Exception, bool>? shouldSuppress = null,
        CancellationToken cancellationToken = default);
}
