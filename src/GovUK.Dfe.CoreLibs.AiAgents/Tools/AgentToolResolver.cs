using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools;

/// <summary>Resolves an agent's tools from its bound providers.</summary>
public static class AgentToolResolver
{
    /// <summary>Groups bindings by agent name, for repeated lookups.</summary>
    public static IReadOnlyDictionary<string, List<IAgentToolProvider>> GroupByAgentName(IEnumerable<AgentToolBinding>? bindings)
        => (bindings ?? [])
            .GroupBy(static binding => binding.AgentName)
            .ToDictionary(static group => group.Key, static group => group.Select(static binding => binding.Provider).ToList());

    /// <summary>Resolves every tool the agent's bound providers offer, with no filtering.</summary>
    public static async Task<IReadOnlyList<ResponseTool>> ResolveAsync(
        IReadOnlyDictionary<string, List<IAgentToolProvider>> toolProvidersByAgentName, string agentName, CancellationToken cancellationToken)
    {
        if (!toolProvidersByAgentName.TryGetValue(agentName, out var providers))
        {
            return [];
        }

        var toolLists = await Task.WhenAll(providers.Select(provider => provider.GetToolsAsync(cancellationToken))).ConfigureAwait(false);
        return [.. toolLists.SelectMany(tools => tools)];
    }

    /// <summary>
    /// Resolves the tools for <paramref name="definition"/>: built-in Foundry tools from its bindings, plus
    /// only the function tools named in <see cref="AgentDefinition.AllowedTools"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">An allowed tool isn't offered by any of the agent's bindings.</exception>
    public static async Task<IReadOnlyList<ResponseTool>> ResolveAsync(
        IReadOnlyDictionary<string, List<IAgentToolProvider>> toolProvidersByAgentName, AgentDefinition definition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var offered = await ResolveAsync(toolProvidersByAgentName, definition.Name, cancellationToken).ConfigureAwait(false);
        return FilterToAllowed(offered, definition);
    }

    /// <summary>Keeps built-in tools and only the function tools in <see cref="AgentDefinition.AllowedTools"/>.</summary>
    /// <exception cref="InvalidOperationException">An allowed tool isn't among <paramref name="offered"/>.</exception>
    public static IReadOnlyList<ResponseTool> FilterToAllowed(IReadOnlyList<ResponseTool> offered, AgentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(offered);
        ArgumentNullException.ThrowIfNull(definition);

        var allowed = definition.AllowedTools.ToHashSet(StringComparer.Ordinal);

        var missing = allowed.Where(name => !offered.OfType<FunctionTool>().Any(tool => tool.FunctionName == name)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(string.Format(ErrorMessages.AllowedToolNotOffered, definition.Name, string.Join(", ", missing)));
        }

        return [.. offered.Where(tool => tool is not FunctionTool function || allowed.Contains(function.FunctionName))];
    }

    /// <summary>
    /// Creates the callback that runs the agent's tool calls in this app, from its bound providers.
    /// See <see cref="AgentToolExecution.CreateResolver"/>.
    /// </summary>
    /// <returns>The callback, or <see langword="null"/> when none of the agent's providers runs tools in this app.</returns>
    public static ToolCallResolver? CreateToolCallResolver(
        IReadOnlyDictionary<string, List<IAgentToolProvider>> toolProvidersByAgentName, string agentName)
        => toolProvidersByAgentName.TryGetValue(agentName, out var providers) ? AgentToolExecution.CreateResolver(providers) : null;

    /// <summary>
    /// Creates the callback that runs <paramref name="definition"/>'s tool calls in this app, refusing any
    /// call to a tool not in its <see cref="AgentDefinition.AllowedTools"/>.
    /// </summary>
    /// <returns>The callback, or <see langword="null"/> when the agent may call no in-app tools.</returns>
    public static ToolCallResolver? CreateToolCallResolver(
        IReadOnlyDictionary<string, List<IAgentToolProvider>> toolProvidersByAgentName, AgentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return definition.AllowedTools.Count > 0 && toolProvidersByAgentName.TryGetValue(definition.Name, out var providers)
            ? AgentToolExecution.CreateResolver(providers, definition.AllowedTools)
            : null;
    }
}
