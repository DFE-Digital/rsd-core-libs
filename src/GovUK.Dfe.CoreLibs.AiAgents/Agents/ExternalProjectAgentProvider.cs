using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

/// <summary>An agent in another Foundry project: resolved and run there, never created.</summary>
/// <param name="version">The version to run; null follows the latest.</param>
internal sealed class ExternalProjectAgentProvider(string agentName, string? version, Func<ExternalFoundryProject> project)
    : IManagedAgentProvider
{
    private readonly Lazy<ExternalFoundryProject> _project = new(project);

    public string AgentName => agentName;

    public bool CreatesAgent => false;

    public string? Version => version;

    public ExternalFoundryProject Project => _project.Value;

    public Task<AgentSpec?> BuildSpecAsync(CancellationToken cancellationToken = default) => Task.FromResult<AgentSpec?>(null);

    public Task<AgentReference> GetAgentAsync(CancellationToken cancellationToken = default)
        => Project.Factory.ResolveAsync(agentName, version, cancellationToken);

    public Task<AgentReference> GetLatestAgentAsync(CancellationToken cancellationToken = default)
        => Project.Factory.ResolveLatestAsync(agentName, cancellationToken);
}
