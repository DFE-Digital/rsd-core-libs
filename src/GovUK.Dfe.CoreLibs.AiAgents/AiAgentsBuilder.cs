using Azure.Core;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Tools;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.AiAgents;

/// <summary>Adds an app's agents, extra tools and code-only settings in <c>AddAiAgents</c>.</summary>
public sealed class AiAgentsBuilder
{
    private readonly List<Action<AiAgentsOptions>> _configureOptions = [];
    private readonly List<AgentDefinition> _definitions = [];

    internal AiAgentsBuilder(IServiceCollection services) => Services = services;

    /// <summary>The service collection, for anything the builder doesn't cover.</summary>
    public IServiceCollection Services { get; }

    internal IReadOnlyList<AgentDefinition> Definitions => _definitions;

    /// <summary>
    /// Registers the app's agents. MCP tools named in their <see cref="AgentDefinition.AllowedTools"/> are
    /// bound automatically to the configured server that allows them.
    /// </summary>
    /// <exception cref="ArgumentException">Two definitions share a name.</exception>
    public AiAgentsBuilder AddAgents(params AgentDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        foreach (var definition in definitions)
        {
            if (_definitions.Exists(existing => existing.Name == definition.Name))
            {
                throw new ArgumentException(string.Format(Constants.ErrorMessages.DuplicateAgentDefinition, definition.Name), nameof(definitions));
            }

            _definitions.Add(definition);
        }

        return this;
    }

    /// <summary>Gives an agent tools that don't come from an MCP server, e.g. <c>new WebSearchToolProvider()</c>.</summary>
    public AiAgentsBuilder AddTools(string agentName, IAgentToolProvider provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        ArgumentNullException.ThrowIfNull(provider);

        Services.AddSingleton(new AgentToolBinding(agentName, provider));
        return this;
    }

    /// <summary>Builds an agent's spec in code, e.g. to use another model. Subclass <c>ManagedAgentProviderBase</c>.</summary>
    public AiAgentsBuilder AddAgentProvider<TProvider>() where TProvider : class, IManagedAgentProvider
    {
        Services.AddSingleton<IManagedAgentProvider, TProvider>();
        return this;
    }

    /// <summary>
    /// Deletes this app's orphaned ephemeral agents every <paramref name="interval"/> (default 30 minutes) in the
    /// background. Optional: without it, schedule <c>IAgentRuntime.DeleteOrphanedEphemeralAgentsAsync</c> yourself.
    /// </summary>
    public AiAgentsBuilder AddEphemeralAgentSweep(TimeSpan? interval = null)
    {
        var every = interval ?? TimeSpan.FromMinutes(30);
        if (every <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "The interval must be positive.");
        }

        Services.AddHostedService(sp => new Agents.EphemeralAgentSweepService(sp.GetRequiredService<Agents.Interfaces.IAgentRuntime>(),
            every, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Agents.EphemeralAgentSweepService>>()));
        return this;
    }

    /// <summary>
    /// Scores answers with <paramref name="evaluator"/>: a <paramref name="sampleRate"/> share of live runs in the
    /// background (<c>aiagents.quality.score</c>), and every <c>IAgentTestRunner</c> case. A rate of 0 scores only tests.
    /// </summary>
    public AiAgentsBuilder AddQualityEvaluation(Func<IServiceProvider, Quality.IAgentRunEvaluator> evaluator, double sampleRate = 0.05)
    {
        ArgumentNullException.ThrowIfNull(evaluator);
        ArgumentOutOfRangeException.ThrowIfNegative(sampleRate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sampleRate, 1);

        Services.AddSingleton(evaluator);
        if (sampleRate > 0)
        {
            Services.AddSingleton(sp => new Quality.AgentQualityMonitor(sp.GetRequiredService<Quality.IAgentRunEvaluator>(), sampleRate,
                sp.GetRequiredService<AgentRunOptions>(), sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Quality.AgentQualityMonitor>>()));
            Services.AddHostedService(sp => sp.GetRequiredService<Quality.AgentQualityMonitor>());
        }

        return this;
    }

    /// <summary>The default credential, instead of <c>AiAgents:Authentication</c>, e.g. a managed identity.</summary>
    public AiAgentsBuilder UseCredential(TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        return Configure(options => options.Credential = credential);
    }

    /// <summary>A credential for one service only. Overrides its <c>Authentication</c> block and the default.</summary>
    public AiAgentsBuilder UseCredentialFor(AiAgentsService service, TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        return Configure(options => options.CredentialOverrides[service.ToString()] = credential);
    }

    /// <summary>A credential for one MCP server (its key under <c>McpServers</c>). Overrides its block and the default.</summary>
    public AiAgentsBuilder UseMcpCredential(string serverName, TokenCredential credential)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        ArgumentNullException.ThrowIfNull(credential);
        return Configure(options => options.CredentialOverrides[AiAgentsOptions.McpCredentialKey(serverName)] = credential);
    }

    /// <summary>Changes settings in code, after they're read from configuration.</summary>
    public AiAgentsBuilder Configure(Action<AiAgentsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configureOptions.Add(configure);
        return this;
    }

    internal void ApplyTo(AiAgentsOptions options) => _configureOptions.ForEach(configure => configure(options));
}
