using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Tools;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

/// <summary>
/// At startup, checks that this app can run every tool its agents will call - for agents it didn't build
/// itself (pinned, or provisioned centrally and used through <see cref="ExternallyManagedAgentProvider"/>).
/// Each tool in the deployed version must be in the agent's <see cref="AgentDefinition.AllowedTools"/> and
/// offered by one of this app's <see cref="AgentToolBinding"/>s. A mismatch fails startup with one error
/// listing every problem, instead of tool calls failing one by one at run time.
/// </summary>
/// <remarks>
/// Agents this app builds itself are skipped: their tools come from the same bindings, so they always match.
/// If Foundry or a tool server can't be reached at startup, a warning is logged and startup continues.
/// </remarks>
internal sealed class AgentToolCompatibilityValidator(IServiceProvider services, AgentRunOptions runOptions,
    IAgentDefinitionProvider? definitionProvider = null, AgentVersionPinningOptions? versionPinning = null,
    IEnumerable<IManagedAgentProvider>? managedAgentProviders = null, IEnumerable<AgentToolBinding>? toolBindings = null,
    ILogger<AgentToolCompatibilityValidator>? logger = null) : IHostedService
{
    // Resolved only when there's something to check, so an app with no pinned or external agents never
    // builds the Foundry client just for this check.
    private IAgentFactory AgentFactory => (IAgentFactory)services.GetService(typeof(IAgentFactory))!;

    private readonly ILogger<AgentToolCompatibilityValidator> _logger = logger ?? NullLogger<AgentToolCompatibilityValidator>.Instance;
    private readonly IReadOnlyDictionary<string, List<IAgentToolProvider>> _toolProviders = AgentToolResolver.GroupByAgentName(toolBindings);
    private readonly HashSet<string> _externallyManaged =
        [.. (managedAgentProviders ?? []).Where(static provider => !provider.CreatesAgent).Select(static provider => provider.AgentName)];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!runOptions.ValidateAgentToolsAtStartup || definitionProvider is null)
        {
            return;
        }

        var problems = new List<string>();
        foreach (var definition in definitionProvider.GetAgentsDefinitions().Where(static definition => definition.IsManagedAgent))
        {
            if (await CheckAsync(definition, cancellationToken).ConfigureAwait(false) is { } problem)
            {
                problems.Add(problem);
            }
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, problems));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<string?> CheckAsync(AgentDefinition definition, CancellationToken cancellationToken)
    {
        var pinnedVersion = versionPinning?.GetPinnedVersion(definition.Name);
        if (pinnedVersion is null && !_externallyManaged.Contains(definition.Name))
        {
            return null;
        }

        IReadOnlyList<string> deployedTools;
        HashSet<string> offeredTools;
        try
        {
            deployedTools = await AgentFactory.GetFunctionToolNamesAsync(definition.Name, pinnedVersion, cancellationToken).ConfigureAwait(false);
            if (deployedTools.Count == 0)
            {
                return null;
            }

            var offered = await AgentToolResolver.ResolveAsync(_toolProviders, definition.Name, cancellationToken).ConfigureAwait(false);
            offeredTools = [.. offered.OfType<FunctionTool>().Select(static tool => tool.FunctionName)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Couldn't check the tools of agent {AgentName} at startup; they'll be checked on each call instead", definition.Name);
            return null;
        }

        var notRunnable = deployedTools.Where(tool => !definition.AllowedTools.Contains(tool) || !offeredTools.Contains(tool)).ToList();
        return notRunnable.Count == 0
            ? null
            : string.Format(ErrorMessages.AgentToolsNotRunnable, definition.Name, pinnedVersion ?? "latest", string.Join(", ", notRunnable));
    }
}
