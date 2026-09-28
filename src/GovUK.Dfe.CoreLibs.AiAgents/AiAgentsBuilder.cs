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

    /// <summary>Uses this credential instead of the configured service principal, e.g. a managed identity.</summary>
    public AiAgentsBuilder UseCredential(TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        return Configure(options => options.Credential = credential);
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
