using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration.Inerfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

public sealed class AgentRuntime(IAgentFactory factory, IAgentRunner runner, IAgentOrchestrator orchestrator,
    AgentVersionPinningOptions? versionPinning = null, ILogger<AgentRuntime>? logger = null) : IAgentRuntime
{
    private readonly AgentVersionPinningOptions _versionPinning = versionPinning ?? new AgentVersionPinningOptions();
    private readonly ILogger<AgentRuntime> _logger = logger ?? NullLogger<AgentRuntime>.Instance;
     
    public IAgentOrchestrator Orchestrator => orchestrator;

    public Task<AgentReference> ResolveAsync(string agentName, CancellationToken cancellationToken)
        => factory.ResolveAsync(agentName, _versionPinning.GetPinnedVersion(agentName), cancellationToken);

    public Task<AgentReference> ResolveAsync(AgentReference created, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(created);

        var pinnedVersion = _versionPinning.GetPinnedVersion(created.Name);
        return pinnedVersion is null
            ? Task.FromResult(created)
            : factory.ResolveAsync(created.Name, pinnedVersion, cancellationToken);
    }

    public async Task<AgentResult> RunEphemeralAsync(AgentSpec spec, string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var reportedName = spec.Name;
        var ephemeralSpec = spec with { Name = $"{spec.Name}-{Guid.NewGuid():N}" };

        try
        {
            var result = await runner.RunAsync(ephemeralSpec, prompt, cancellationToken: cancellationToken).ConfigureAwait(false);
            return result with { AgentName = reportedName };
        }
        finally
        {
            try
            {
                await factory.DeleteAgentAsync(ephemeralSpec.Name, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { 
                _logger.LogWarning(ex, "Failed to delete ephemeral agent {AgentName}; it may be orphaned in Foundry.", ephemeralSpec.Name);
            }
        }
    }
}
