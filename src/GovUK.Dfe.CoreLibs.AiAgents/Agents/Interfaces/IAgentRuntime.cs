using GovUK.Dfe.CoreLibs.AiAgents.Orchestration.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>
/// The runtime environment for agents, providing methods to resolve, create, and run agents, as well as manage ephemeral agents.
/// </summary>
public interface IAgentRuntime
{
    IAgentOrchestrator Orchestrator { get; }

    /// <summary>
    /// Resolves an agent by name, using the pinned version if one is specified for the environment.
    /// </summary>
    /// <param name="agentName">The name of the agent to resolve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved agent reference.</returns>
    Task<AgentReference> ResolveAsync(string agentName, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves an agent reference to the pinned version, if any.
    /// </summary>
    /// <param name="created">The agent reference to resolve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved agent reference.</returns>
    Task<AgentReference> ResolveAsync(AgentReference created, CancellationToken cancellationToken);

    /// <summary>
    /// Gets an existing agent by name, or creates a new one using the provided build specification if it doesn't exist.
    /// </summary>
    /// <param name="agentName">The name of the agent to get or create.</param>
    /// <param name="buildSpec">A function that builds the agent's current specification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent reference.</returns>

    Task<AgentReference> GetOrCreateAsync(string agentName, Func<CancellationToken, Task<AgentSpec>> buildSpec,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs an agent with a unique ephemeral name, then deletes it from Foundry.
    /// </summary>
    /// <param name="spec">The agent specification.</param>
    /// <param name="prompt">The prompt to run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent's result.</returns>
    Task<AgentResult> RunEphemeralAsync(AgentSpec spec, string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs an agent with a unique ephemeral name, running its function tool calls with
    /// <paramref name="resolveToolCalls"/>, then deletes it from Foundry.
    /// </summary>
    /// <param name="spec">The agent specification.</param>
    /// <param name="prompt">The prompt to run.</param>
    /// <param name="resolveToolCalls">Runs the model's function tool calls in this app, or null when the agent has none.</param>
    /// <param name="evidence">Untrusted evidence, sent fenced as data; null for none.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent's result.</returns>
    Task<AgentResult> RunEphemeralAsync(AgentSpec spec, string prompt,
        Func<IReadOnlyList<ToolCallRequest>, CancellationToken, Task<IEnumerable<ToolCallOutput>>>? resolveToolCalls,
        string? evidence = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes ephemeral agents that have been orphaned for longer than <paramref name="minimumAge"/>, returning their names.
    /// </summary>
    /// <param name="minimumAge">The minimum age of agents to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The names of the agents deleted.</returns>
    Task<IReadOnlyList<string>> DeleteOrphanedEphemeralAgentsAsync(TimeSpan minimumAge, CancellationToken cancellationToken = default);
}
