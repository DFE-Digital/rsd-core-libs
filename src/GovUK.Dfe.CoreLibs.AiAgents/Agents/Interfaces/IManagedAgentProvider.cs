using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>
/// Defines a provider for managed agents, responsible for ensuring the agent exists and resolving its reference, with support for versioning and version pinning.
/// </summary>
public interface IManagedAgentProvider
{
    /// <summary>
    /// The agent's name in Foundry.
    /// </summary>
    string AgentName { get; }

    /// <summary>
    /// Ensures the agent exists (creating or versioning it if needed), then resolves its reference, respecting any configured version pin.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AgentReference> GetAgentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the agent exists (creating or versioning it if needed), then resolves its reference to the latest version, ignoring any configured version pin.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AgentReference> GetLatestAgentAsync(CancellationToken cancellationToken = default);
}
