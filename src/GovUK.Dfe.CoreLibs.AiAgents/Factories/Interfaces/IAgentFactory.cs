using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;

/// <summary>
/// Defines a factory for creating and resolving agents based on their specifications and versioning.
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
    /// Deletes all but the latest <paramref name="keepLatestVersions"/> versions of an agent.
    /// </summary>
    /// <param name="name">The agent name.</param>
    /// <param name="keepLatestVersions">The number of latest versions to keep.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PruneVersionsAsync(string name, int keepLatestVersions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the deployed version of an agent matches the provided specification.
    /// </summary>
    /// <param name="spec">The agent specification to compare against the deployed version.</param>
    /// <param name="version">The deployed version to compare against.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> if the deployed version's content matches <paramref name="spec"/>.</returns>
    Task<bool> MatchesDeployedVersionAsync(AgentSpec spec, string version, CancellationToken cancellationToken = default);
}
