using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;

/// <summary>
/// Advanced: creates, resolves, deletes and prunes agent versions in Foundry. <c>IAgentService</c> uses it
/// for you; call it directly for maintenance such as <see cref="PruneVersionsAsync"/>.
/// </summary>
public interface IAgentFactory
{
    /// <summary>
    /// Gets an existing agent matching the specification, or creates a new version if the Model, Instructions, or Tools have changed.
    /// </summary>
    /// <param name="spec">The agent specification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent reference.</returns>
    Task<AgentReference> GetOrCreateAsync(AgentSpec spec, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an existing agent by name and optionally pins to a specific version.
    /// </summary>
    /// <param name="name">The agent name.</param>
    /// <param name="version">The version to pin to, or null for the latest version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved agent reference.</returns>
    /// <exception cref="InvalidOperationException">The agent or specified version does not exist.</exception>
    Task<AgentReference> ResolveAsync(string name, string? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an existing agent by name, always returning its latest version regardless of any
    /// configured version pin. Equivalent to <c>ResolveAsync(name, version: null, cancellationToken)</c>.
    /// </summary>
    /// <param name="name">The agent name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved agent reference, at its latest version.</returns>
    /// <exception cref="InvalidOperationException">The agent does not exist.</exception>
    Task<AgentReference> ResolveLatestAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an agent and all its versions, invalidating any references and cached entries for the agent name.
    /// </summary>
    /// <param name="name">The agent name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteAgentAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all but the newest <paramref name="keepLatestVersions"/> versions of an agent - e.g. 3 keeps
    /// 5, 4 and 3 and deletes 1 and 2. Does nothing for an agent this environment has pinned, and never
    /// deletes a version in <c>ProtectedVersions</c>.
    /// </summary>
    /// <param name="name">The agent name.</param>
    /// <param name="keepLatestVersions">How many of the newest versions to keep; at least 1.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PruneVersionsAsync(string name, int keepLatestVersions, CancellationToken cancellationToken = default);

    /// <summary>
    /// The function tools a deployed version expects this app to run.
    /// </summary>
    /// <param name="name">The agent name.</param>
    /// <param name="version">The version, or null for the latest.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="InvalidOperationException">The agent or version doesn't exist.</exception>
    Task<IReadOnlyList<string>> GetFunctionToolNamesAsync(string name, string? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the deployed version of an agent matches the provided specification.
    /// </summary>
    /// <param name="spec">The agent specification to compare against the deployed version.</param>
    /// <param name="version">The deployed version to compare against.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> if the deployed version's content matches <paramref name="spec"/>.</returns>
    Task<bool> MatchesDeployedVersionAsync(AgentSpec spec, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes every agent whose name matches <paramref name="isCandidate"/> and whose latest version is
    /// older than <paramref name="minimumAge"/>. Used to clean up agents a crashed or failed run left behind.
    /// </summary>
    /// <param name="isCandidate">Decides, by name, whether an agent may be deleted.</param>
    /// <param name="minimumAge">How old an agent's latest version must be, so agents still in use are left alone.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The names of the agents deleted.</returns>
    Task<IReadOnlyList<string>> DeleteStaleAgentsAsync(Func<string, bool> isCandidate, TimeSpan minimumAge,
        CancellationToken cancellationToken = default);
}
