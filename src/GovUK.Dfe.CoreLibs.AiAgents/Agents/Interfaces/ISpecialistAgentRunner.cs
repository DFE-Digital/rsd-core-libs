using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>
/// Defines a runner for specialist agents, which can be either managed or ephemeral. Managed agents are orchestrated through <see cref="IAgentRuntime.Orchestrator"/>, while ephemeral agents are run directly via <see cref="IAgentRuntime.RunAsync"/>.
/// </summary>
public interface ISpecialistAgentRunner
{
    /// <summary>
    /// Runs a collection of specialist agents in parallel, resolving their prompts and executing them within the provided context. The method returns a list of results from each agent execution.
    /// </summary>
    /// <param name="definitions">The specialist agents to run.</param>
    /// <param name="resolvePrompt">A function to resolve the prompt for each agent.</param>
    /// <param name="context">The context in which to run the agents.</param>
    /// <param name="shouldSuppress">A function to determine whether to suppress exceptions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of results from each agent execution.</returns>
    Task<IReadOnlyList<AgentResult>> RunParallelAsync(IReadOnlyCollection<AgentDefinition> definitions,
        Func<AgentDefinition, CancellationToken, Task<string>> resolvePrompt, AgentContext context,
        Func<Exception, bool>? shouldSuppress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a collection of specialist agents sequentially, resolving their prompts and executing them within the provided context. The method returns a list of results from each agent execution.
    /// </summary>
    /// <param name="definitions">The specialist agents to run.</param>
    /// <param name="resolvePrompt">A function to resolve the prompt for each agent.</param>
    /// <param name="initialInput">The initial input for the first agent.</param>
    /// <param name="context">The context in which to run the agents.</param>
    /// <param name="shouldSuppress">A function to determine whether to suppress exceptions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of results from each agent execution.</returns>
    Task<IReadOnlyList<AgentResult>> RunSequentialAsync(IReadOnlyList<AgentDefinition> definitions,
        Func<AgentDefinition, string, CancellationToken, Task<string>> resolvePrompt, string initialInput,
        AgentContext context, Func<Exception, bool>? shouldSuppress = null, CancellationToken cancellationToken = default);
}
