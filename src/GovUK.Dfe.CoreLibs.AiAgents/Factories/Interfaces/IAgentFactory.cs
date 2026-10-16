using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;

/// <summary>Advanced: agent versions in Foundry. <c>IAgentService</c> uses it for you; use it directly for maintenance.</summary>
public interface IAgentFactory
{
    /// <summary>Reuses a version matching <paramref name="spec"/>'s model, instructions, tools and schema, or creates one.</summary>
    Task<AgentReference> GetOrCreateAsync(AgentSpec spec, CancellationToken cancellationToken = default);

    /// <summary>The given <paramref name="version"/>, or the latest when null.</summary>
    /// <exception cref="InvalidOperationException">The agent or version doesn't exist.</exception>
    Task<AgentReference> ResolveAsync(string name, string? version = null, CancellationToken cancellationToken = default);

    /// <summary>The latest version, ignoring any pin.</summary>
    /// <exception cref="InvalidOperationException">The agent doesn't exist.</exception>
    Task<AgentReference> ResolveLatestAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Deletes the agent and all its versions. Succeeds if it's already gone.</summary>
    Task DeleteAgentAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Keeps the newest <paramref name="keepLatestVersions"/> (at least 1) and deletes the rest: 3 of 1–5 keeps 5, 4, 3.
    /// Does nothing for a pinned agent, and never deletes a protected version.
    /// </summary>
    Task PruneVersionsAsync(string name, int keepLatestVersions, CancellationToken cancellationToken = default);

    /// <summary>The function tools a version (latest when null) expects this app to run.</summary>
    /// <exception cref="InvalidOperationException">The agent or version doesn't exist.</exception>
    Task<IReadOnlyList<string>> GetFunctionToolNamesAsync(string name, string? version = null, CancellationToken cancellationToken = default);

    /// <summary>Whether the deployed <paramref name="version"/> matches <paramref name="spec"/>. False if it doesn't exist.</summary>
    Task<bool> MatchesDeployedVersionAsync(AgentSpec spec, string version, CancellationToken cancellationToken = default);

    /// <summary>Deletes agents whose name passes <paramref name="isCandidate"/> and whose latest version is older than <paramref name="minimumAge"/>.</summary>
    /// <returns>The names deleted.</returns>
    Task<IReadOnlyList<string>> DeleteStaleAgentsAsync(Func<string, bool> isCandidate, TimeSpan minimumAge,
        CancellationToken cancellationToken = default);
}
