using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

public abstract class ManagedAgentProviderBase(IAgentFactory factory, IAgentRuntime runtime) : IManagedAgentProvider
{ 
    public abstract string AgentName { get; }

    protected abstract AgentSpec BuildSpec();
     
    public async Task<AgentReference> GetAgentAsync(CancellationToken cancellationToken = default)
    {
        var created = await factory.GetOrCreateAsync(BuildSpec(), cancellationToken).ConfigureAwait(false);
        return await runtime.ResolveAsync(created, cancellationToken).ConfigureAwait(false);
    }
     
    public Task<AgentReference> GetLatestAgentAsync(CancellationToken cancellationToken = default) 
        => factory.GetOrCreateAsync(BuildSpec(), cancellationToken);
}
