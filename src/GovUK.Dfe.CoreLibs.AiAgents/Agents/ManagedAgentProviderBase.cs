using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

public abstract class ManagedAgentProviderBase(IAgentFactory factory, IAgentRuntime runtime) : IManagedAgentProvider
{ 
    public abstract string AgentName { get; }

    protected abstract AgentSpec BuildSpec();

    public Task<AgentSpec?> BuildSpecAsync(CancellationToken cancellationToken = default) => Task.FromResult<AgentSpec?>(BuildSpec());
     
    /// <summary>
    /// Gets the agent to run: the pinned version when one is configured (without building the spec or
    /// creating anything), otherwise the version matching <see cref="BuildSpec"/>, created if needed.
    /// </summary>
    public Task<AgentReference> GetAgentAsync(CancellationToken cancellationToken = default)
        => runtime.GetOrCreateAsync(AgentName, _ => Task.FromResult(BuildSpec()), cancellationToken);

    /// <summary>Gets or creates the version matching <see cref="BuildSpec"/>, ignoring any pin.</summary>
    public Task<AgentReference> GetLatestAgentAsync(CancellationToken cancellationToken = default) 
        => factory.GetOrCreateAsync(BuildSpec(), cancellationToken);
}
