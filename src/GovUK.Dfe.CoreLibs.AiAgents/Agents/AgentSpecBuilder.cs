using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Tools;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

/// <summary>
/// Builds an <see cref="AgentDefinition"/>'s spec the same way everywhere: from its system prompt and tool
/// bindings, or from its custom provider. The definition's allowed tools and output schema apply either way.
/// </summary>
internal sealed class AgentSpecBuilder(IPromptProvider promptProvider, IEnumerable<AgentToolBinding>? toolBindings = null,
    IEnumerable<IManagedAgentProvider>? customProviders = null)
{
    private readonly Dictionary<string, IManagedAgentProvider> _customProviders =
        (customProviders ?? []).ToDictionary(static provider => provider.AgentName);

    /// <summary>Each agent's bound tool providers, by agent name.</summary>
    public IReadOnlyDictionary<string, List<IAgentToolProvider>> ToolProviders { get; } = AgentToolResolver.GroupByAgentName(toolBindings);

    public bool IsExternallyManaged(AgentDefinition definition)
        => _customProviders.TryGetValue(definition.Name, out var provider) && !provider.CreatesAgent;

    public IManagedAgentProvider? CustomProviderFor(AgentDefinition definition)
        => _customProviders.GetValueOrDefault(definition.Name);

    /// <returns>The spec, or <see langword="null"/> for an externally managed agent.</returns>
    public async Task<AgentSpec?> BuildAsync(AgentDefinition definition, CancellationToken cancellationToken)
    {
        if (_customProviders.TryGetValue(definition.Name, out var provider))
        {
            var custom = await provider.BuildSpecAsync(cancellationToken).ConfigureAwait(false);
            return custom is null
                ? null
                : custom with
                {
                    Tools = AgentToolResolver.FilterToAllowed(custom.Tools, definition),
                    OutputSchema = custom.OutputSchema ?? definition.OutputSchema,
                };
        }

        return new AgentSpec
        {
            Name = definition.Name,
            Instructions = promptProvider.GetSystemPrompt(definition.SystemPromptKey),
            Tools = await AgentToolResolver.ResolveAsync(ToolProviders, definition, cancellationToken).ConfigureAwait(false),
            OutputSchema = definition.OutputSchema,
        };
    }
}
