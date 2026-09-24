using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Tools;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

/// <summary>
/// Hosted service that checks for drift between the pinned version of each managed agent and its current definition.
/// </summary>
/// <param name="definitionProvider">The provider for retrieving agent definitions.</param>
/// <param name="versionPinning">The options for version pinning.</param>
/// <param name="agentFactory">The factory for creating agent instances.</param>
/// <param name="promptProvider">The provider for retrieving system prompts.</param>
/// <param name="managedAgentProviders">The providers for managed agents.</param>
/// <param name="toolBindings">The tool bindings.</param>
/// <param name="logger">The logger.</param>
public sealed class PinnedAgentVersionDriftValidator(IAgentDefinitionProvider definitionProvider, AgentVersionPinningOptions versionPinning,
    IAgentFactory agentFactory, IPromptProvider promptProvider, IEnumerable<IManagedAgentProvider>? managedAgentProviders = null,
    IEnumerable<AgentToolBinding>? toolBindings = null, ILogger<PinnedAgentVersionDriftValidator>? logger = null) : IHostedService
{
    private readonly HashSet<string> _customManagedAgentNames = [.. (managedAgentProviders ?? []).Select(static p => p.AgentName)];
    private readonly IReadOnlyDictionary<string, List<IAgentToolProvider>> _toolProviders = AgentToolResolver.GroupByAgentName(toolBindings);
    private readonly ILogger<PinnedAgentVersionDriftValidator> _logger = logger ?? NullLogger<PinnedAgentVersionDriftValidator>.Instance;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in definitionProvider.GetAgentsDefinitions())
        {
            await CheckDefinitionAsync(definition, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task CheckDefinitionAsync(AgentDefinition definition, CancellationToken cancellationToken)
    {
        if (!definition.IsManagedAgent)
        {
            return;
        }

        var pinnedVersion = versionPinning.GetPinnedVersion(definition.Name);
        if (pinnedVersion is null)
        {
            return;
        }

        if (_customManagedAgentNames.Contains(definition.Name))
        {
            _logger.LogInformation( "Skipping pinned version drift check for '{AgentName}'; it has a custom IManagedAgentProvider whose spec can't be reconstructed generically.",
                definition.Name);
            return;
        }

        var spec = new AgentSpec
        {
            Name = definition.Name,
            Instructions = promptProvider.GetSystemPrompt(definition.SystemPromptType),
            Tools = await AgentToolResolver.ResolveAsync(_toolProviders, definition.Name, cancellationToken).ConfigureAwait(false),
        };

        try
        {
            var matches = await agentFactory.MatchesDeployedVersionAsync(spec, pinnedVersion, cancellationToken).ConfigureAwait(false);
            if (!matches)
            {
                _logger.LogWarning(
                    "Pinned version '{Version}' for agent '{AgentName}' no longer matches its current definition " +
                    "(model, instructions or tools have changed since it was pinned, or that version no longer exists). Consider re-pinning.",
                    pinnedVersion, definition.Name);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not check pinned version drift for agent '{AgentName}' version '{Version}'.",
                definition.Name, pinnedVersion);
        }

        // Separate from the content check above: the pinned version's content can still match the
        // current spec while a newer version number already exists for this agent (created some
        // other way, or by a promotion step that bumped Foundry without re-pinning) - that's worth
        // its own signal, since re-pinning is a decision for whoever owns the environment's config,
        // not something this validator should do on their behalf.
        try
        {
            var latest = await agentFactory.ResolveLatestAsync(definition.Name, cancellationToken).ConfigureAwait(false);
            if (latest.Version != pinnedVersion)
            {
                _logger.LogWarning(
                    "Agent '{AgentName}' is pinned to version '{PinnedVersion}', but Foundry's latest version is '{LatestVersion}'. Consider re-pinning.",
                    definition.Name, pinnedVersion, latest.Version);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not resolve the latest version for agent '{AgentName}' while checking pinned version drift.",
                definition.Name);
        }
    }
}
