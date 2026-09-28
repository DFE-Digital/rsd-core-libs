using GovUK.Dfe.CoreLibs.AiAgents.Orchestration.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>
/// Advanced: pin-aware agent resolution, ephemeral runs and the orphan sweep. Most apps only need
/// <see cref="IAgentService"/>, plus <see cref="DeleteOrphanedEphemeralAgentsAsync"/> from a scheduled job.
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
    /// Gets the agent to run for a managed agent. A pinned agent resolves its pinned version and never
    /// builds a spec or creates anything, so pinned environments need only read access to Foundry. An
    /// unpinned agent is got or created from the spec that <paramref name="buildSpec"/> returns.
    /// </summary>
    /// <param name="agentName">The agent name.</param>
    /// <param name="buildSpec">Builds the agent's current spec. Only called when the agent isn't pinned.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent reference to run.</returns>
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
    /// Deletes ephemeral agents that a crashed process or a failed delete left in Foundry. Only agents
    /// named like an ephemeral agent (<c>&lt;name&gt;-&lt;32 hex digits&gt;</c>) and older than
    /// <paramref name="minimumAge"/> are deleted. Call it from a scheduled job.
    /// </summary>
    /// <param name="minimumAge">
    /// How old an ephemeral agent must be before it's treated as orphaned. Must exceed the longest a run
    /// can take (<see cref="AgentRunOptions.RunTimeout"/>, or one hour when unset) plus clean-up time.
    /// Only this application's ephemeral agents are ever deleted, and it's safe to run on every instance.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The names of the agents deleted.</returns>
    Task<IReadOnlyList<string>> DeleteOrphanedEphemeralAgentsAsync(TimeSpan minimumAge, CancellationToken cancellationToken = default);
}
