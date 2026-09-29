using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>
/// Advanced: supplies a managed agent whose spec the library can't build from its <see cref="AgentDefinition"/>
/// alone (e.g. a different model), or one created outside this app. Subclass <see cref="ManagedAgentProviderBase"/>
/// or use <see cref="ExternallyManagedAgentProvider"/>.
/// </summary>
public interface IManagedAgentProvider
{
    /// <summary>The agent's name in Foundry.</summary>
    string AgentName { get; }

    /// <summary>
    /// Whether this app creates the agent's versions. <see langword="false"/> for agents provisioned
    /// elsewhere, which are only ever resolved.
    /// </summary>
    bool CreatesAgent => true;

    /// <summary>
    /// Builds the agent's spec, or returns <see langword="null"/> when <see cref="CreatesAgent"/> is
    /// <see langword="false"/>. The agent's <see cref="AgentDefinition.AllowedTools"/> and
    /// <see cref="AgentDefinition.OutputSchema"/> are applied to it before use.
    /// </summary>
    Task<AgentSpec?> BuildSpecAsync(CancellationToken cancellationToken = default);

    /// <summary>The agent at its pinned version, creating or versioning it first if needed.</summary>
    Task<AgentReference> GetAgentAsync(CancellationToken cancellationToken = default);

    /// <summary>As above, at the latest version, ignoring any pin.</summary>
    Task<AgentReference> GetLatestAgentAsync(CancellationToken cancellationToken = default);
}
