namespace GovUK.Dfe.CoreLibs.AiAgents.Factories;

/// <summary>
/// Options for pinning specific agents to a specific Foundry version, rather than floating to the latest version.
/// </summary>
public sealed class AgentVersionPinningOptions
{
    public IReadOnlyDictionary<string, string> VersionPins { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the pinned version for the named agent, or null if it isn't pinned and should float
    /// to the latest Foundry version.
    /// </summary>
    public string? GetPinnedVersion(string agentName) =>
        VersionPins.TryGetValue(agentName, out var version) ? version : null;
}
