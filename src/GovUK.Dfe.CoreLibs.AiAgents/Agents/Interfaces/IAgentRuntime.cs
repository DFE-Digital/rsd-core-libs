using GovUK.Dfe.CoreLibs.AiAgents.Orchestration.Inerfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>
/// Bundles the runtime dependencies needed to resolve and run provisioned agents.
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
    /// Runs an agent with a unique ephemeral name, then deletes it from Foundry.
    /// </summary>
    /// <param name="spec">The agent specification.</param>
    /// <param name="prompt">The prompt to run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent's result.</returns>
    Task<AgentResult> RunEphemeralAsync(AgentSpec spec, string prompt, CancellationToken cancellationToken = default);
}
