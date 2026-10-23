using GovUK.Dfe.CoreLibs.AiAgents.Orchestration.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>Advanced: resolving, creating and running agents directly, and cleaning up ephemeral ones.</summary>
public interface IAgentRuntime
{
    IAgentOrchestrator Orchestrator { get; }

    /// <summary>The agent at this environment's pinned version, or its latest.</summary>
    Task<AgentReference> ResolveAsync(string agentName, CancellationToken cancellationToken);

    /// <summary><paramref name="created"/>, or its pinned version if this environment pins it.</summary>
    Task<AgentReference> ResolveAsync(AgentReference created, CancellationToken cancellationToken);

    /// <summary>The pinned version if there is one; otherwise gets or creates the version for <paramref name="buildSpec"/>.</summary>
    Task<AgentReference> GetOrCreateAsync(string agentName, Func<CancellationToken, Task<AgentSpec>> buildSpec,
        CancellationToken cancellationToken = default);

    /// <summary>Creates the agent under a unique name, runs it once, then deletes it.</summary>
    Task<AgentResult> RunEphemeralAsync(AgentSpec spec, string prompt, CancellationToken cancellationToken = default);

    /// <summary>As above, running its tool calls, sending <paramref name="evidence"/> fenced, and retrying one invalid answer.</summary>
    Task<AgentResult> RunEphemeralAsync(AgentSpec spec, string prompt, ToolCallResolver? resolveToolCalls,
        string? evidence = null, Func<AgentResult, string?>? validateOutput = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes this app's ephemeral agents left by crashes or failed deletes. Call it from your own schedule, or add
    /// <c>agents.AddEphemeralAgentSweep()</c>. Only agents older than any run can be are deleted, never another app's;
    /// safe on every instance at once.
    /// </summary>
    /// <returns>The names deleted.</returns>
    Task<IReadOnlyList<string>> DeleteOrphanedEphemeralAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>As above, for agents older than <paramref name="minimumAge"/> (not less than the safe minimum).</summary>
    Task<IReadOnlyList<string>> DeleteOrphanedEphemeralAgentsAsync(TimeSpan minimumAge, CancellationToken cancellationToken = default);
}
