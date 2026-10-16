using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Factories;

/// <summary>Settings for creating and resolving agent versions.</summary>
/// <param name="DefaultModel">
/// The default model deployment used when <see cref="AgentSpec.Model"/> is not set.
/// </param>
public sealed record FoundryAgentFactoryOptions(string DefaultModel)
{
    /// <summary>
    /// When set, each new version prunes older ones, keeping this many (3 keeps 5, 4 and 3). Pinned and protected
    /// versions are kept. Null (default): never prunes.
    /// </summary>
    public int? KeepLatestVersions { get; init; }

    /// <summary>How long this instance reuses a resolved agent version. Zero turns caching off. Default: 30 seconds.</summary>
    public TimeSpan AgentCacheDuration { get; init; } = TimeSpan.FromSeconds(30);
}
