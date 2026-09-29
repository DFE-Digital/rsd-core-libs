using Azure.Core;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Tools;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.AiAgents;

/// <summary>Adds an app's agents, tools and code-only settings in <c>AddAiAgents</c>.</summary>
public sealed class AiAgentsBuilder
{
    private readonly List<Action<AiAgentsOptions>> _configureOptions = [];
    private readonly List<AgentDefinition> _definitions = [];

    internal AiAgentsBuilder(IServiceCollection services) => Services = services;

    /// <summary>The service collection, for anything the builder doesn't cover.</summary>
    public IServiceCollection Services { get; }

    internal IReadOnlyList<AgentDefinition> Definitions => _definitions;

    /// <summary>Adds this app's agents.</summary>
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

    /// <summary>Gives an agent extra tools, e.g. <c>WebSearchToolProvider</c>. Called on every run.</summary>
    public AiAgentsBuilder AddTools(string agentName, IAgentToolProvider provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        ArgumentNullException.ThrowIfNull(provider);

        Services.AddSingleton(new AgentToolBinding(agentName, provider));
        return this;
    }

    /// <summary>Builds an agent in code on every run, e.g. one per user.</summary>
    public AiAgentsBuilder AddAgentProvider<TProvider>() where TProvider : class, IManagedAgentProvider
    {
        Services.AddSingleton<IManagedAgentProvider, TProvider>();
        return this;
    }

    /// <summary>Deletes unused ephemeral agents every <paramref name="interval"/> (default 30 minutes).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The interval isn't positive.</exception>
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
    /// Scores every <c>IAgentTestRunner</c> case, and a <paramref name="sampleRate"/> share of live runs in the background
    /// (<c>aiagents.quality.score</c>). A rate of 0 scores only tests.
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

    /// <summary>
    /// Adds a quality evaluation using a Foundry judge model. The <paramref name="sampleRate"/> is the share of live runs scored in the background (<c>aiagents.quality.score</c>), and every <c>IAgentTestRunner</c> case. A rate of 0 scores only tests.
    /// </summary>
    /// <param name="judgeModel">The Foundry judge model to use for evaluation.</param>
    /// <param name="sampleRate">The rate at which to sample live runs for quality evaluation.</param>
    /// <param name="evaluator">The evaluator to use for quality assessment.</param>
    /// <returns>The builder instance.</returns>
    public AiAgentsBuilder AddQualityEvaluation(string judgeModel, double sampleRate = 0.05,
        Microsoft.Extensions.AI.Evaluation.IEvaluator? evaluator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(judgeModel);

        var scoring = evaluator ?? new Microsoft.Extensions.AI.Evaluation.CompositeEvaluator(
            new Microsoft.Extensions.AI.Evaluation.Quality.GroundednessEvaluator(), new Microsoft.Extensions.AI.Evaluation.Quality.RelevanceEvaluator());

        return AddQualityEvaluation(sp => new Quality.ExtensionsAiEvaluator(scoring,
            new Microsoft.Extensions.AI.Evaluation.ChatConfiguration(
                new Quality.FoundryJudgeChatClient(sp.GetRequiredService<Azure.AI.Extensions.OpenAI.ProjectOpenAIClient>(), judgeModel)),
            sp.GetService<Microsoft.Extensions.Logging.ILogger<Quality.ExtensionsAiEvaluator>>()), sampleRate);
    }

    /// <summary>
    /// Sets the default credential for the AI agents.
    /// </summary>
    /// <param name="credential">The token credential to use.</param>
    /// <returns>The builder instance.</returns>
    public AiAgentsBuilder UseCredential(TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        return Configure(options => options.Credential = credential);
    }

    /// <summary>
    /// Sets the credential for a specific service.
    /// </summary>
    /// <param name="service">The AI agents service.</param>
    /// <param name="credential">The token credential to use.</param>
    /// <returns>The builder instance.</returns>
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

    /// <summary>A credential for the <c>ExternallyManagedAgents</c> project (needs its <c>Endpoint</c>). Overrides its <c>Authentication</c> and this app's Foundry credential.</summary>
    public AiAgentsBuilder UseExternallyManagedAgentsCredential(TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        return Configure(options => options.CredentialOverrides[AiAgentsOptions.ExternallyManagedCredentialKey] = credential);
    }

    /// <summary>Changes settings in code, after configuration is read.</summary>
    public AiAgentsBuilder Configure(Action<AiAgentsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configureOptions.Add(configure);
        return this;
    }

    internal void ApplyTo(AiAgentsOptions options) => _configureOptions.ForEach(configure => configure(options));
}
