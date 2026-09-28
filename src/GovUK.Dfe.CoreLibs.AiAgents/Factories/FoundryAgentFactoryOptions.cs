using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Factories;

/// <summary>
/// Options for configuring the Foundry agent factory.
/// </summary>
/// <param name="DefaultModel">
/// The default model deployment used when <see cref="AgentSpec.Model"/> is not set.
/// </param>
public sealed record FoundryAgentFactoryOptions(string DefaultModel)
{
    /// <summary>
    /// When set, each time this app creates a new version of an agent, older versions are deleted so only
    /// this many of the newest remain - e.g. 3 keeps versions 5, 4 and 3 and deletes 1 and 2. Pinned and
    /// protected versions are always kept. <see langword="null"/> (the default) never prunes automatically.
    /// </summary>
    public int? KeepLatestVersions { get; init; }

    /// <summary>
    /// How long this instance reuses a resolved agent version without asking Foundry again. Saves one or more
    /// Foundry calls per run. <see cref="TimeSpan.Zero"/> turns caching off. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan AgentCacheDuration { get; init; } = TimeSpan.FromSeconds(30);
}
