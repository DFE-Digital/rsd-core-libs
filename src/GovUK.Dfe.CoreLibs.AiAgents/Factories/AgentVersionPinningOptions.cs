namespace GovUK.Dfe.CoreLibs.AiAgents.Factories;

/// <summary>
/// Which agent versions this environment runs, and which must never be deleted.
/// </summary>
public sealed class AgentVersionPinningOptions
{
    /// <summary>The version this environment runs, per agent. An agent with no pin uses its latest matching version.</summary>
    public IReadOnlyDictionary<string, string> VersionPins { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Versions pruning must never delete, per agent - typically the versions other environments are pinned
    /// to when several environments share one Foundry project.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ProtectedVersions { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>
    /// Gets the pinned version for the named agent, or null if it isn't pinned and should float
    /// to the latest Foundry version.
    /// </summary>
    public string? GetPinnedVersion(string agentName) =>
        VersionPins.TryGetValue(agentName, out var version) ? version : null;

    /// <summary>Whether pruning must keep this version: it's pinned here or protected for another environment.</summary>
    public bool IsProtected(string agentName, string version)
        => GetPinnedVersion(agentName) == version
           || (ProtectedVersions.TryGetValue(agentName, out var protectedVersions) && protectedVersions.Contains(version));
}
