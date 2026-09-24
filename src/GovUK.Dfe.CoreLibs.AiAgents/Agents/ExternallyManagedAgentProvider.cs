using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

public sealed class ExternallyManagedAgentProvider(string agentName, IAgentFactory agentFactory, IAgentRuntime agentRuntime)
    : IManagedAgentProvider
{
    public string AgentName => agentName;

    /// <summary>Resolves the agent by name, applying the configured version pin (or floating to latest if unpinned).</summary>
    public Task<AgentReference> GetAgentAsync(CancellationToken cancellationToken = default)
        => agentRuntime.ResolveAsync(agentName, cancellationToken);

    /// <summary>Resolves the agent's latest version, ignoring any configured pin.</summary>
    public Task<AgentReference> GetLatestAgentAsync(CancellationToken cancellationToken = default)
        => agentFactory.ResolveLatestAsync(agentName, cancellationToken);
}
