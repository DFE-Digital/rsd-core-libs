namespace GovUK.Dfe.CoreLibs.AiAgents.Factories;

/// <summary>Which agent versions this environment runs, and which must never be deleted.</summary>
public sealed class AgentVersionPinningOptions
{
    /// <summary>The version this environment runs, per agent. An agent with no pin uses its latest matching version.</summary>
    public IReadOnlyDictionary<string, string> VersionPins { get; init; } = new Dictionary<string, string>();

    /// <summary>Versions pruning must keep, per agent, e.g. those other environments sharing the project are pinned to.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ProtectedVersions { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>The agent's pinned version, or null to run the latest.</summary>
    public string? GetPinnedVersion(string agentName) =>
        VersionPins.TryGetValue(agentName, out var version) ? version : null;

    /// <summary>Whether pruning must keep this version: it's pinned here or protected for another environment.</summary>
    public bool IsProtected(string agentName, string version)
        => GetPinnedVersion(agentName) == version
           || (ProtectedVersions.TryGetValue(agentName, out var protectedVersions) && protectedVersions.Contains(version));
}
