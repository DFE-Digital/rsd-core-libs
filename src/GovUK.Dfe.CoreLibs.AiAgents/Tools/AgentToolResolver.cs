using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools;

/// <summary>
/// Resolves the tools available to an agent, based on its name and the registered providers.
/// </summary>
public static class AgentToolResolver
{
    /// <summary>Groups a set of bindings by agent name, for repeated lookups via <see cref="ResolveAsync"/>.</summary>
    public static IReadOnlyDictionary<string, List<IAgentToolProvider>> GroupByAgentName(IEnumerable<AgentToolBinding>? bindings)
        => (bindings ?? [])
            .GroupBy(static binding => binding.AgentName)
            .ToDictionary(static group => group.Key, static group => group.Select(static binding => binding.Provider).ToList());

    /// <summary>
    /// Resolves the tools available to an agent, based on its name and the registered providers.
    /// </summary>
    /// <param name="toolProvidersByAgentName">A dictionary mapping agent names to their respective tool providers.</param>
    /// <param name="agentName">The name of the agent for which to resolve tools.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of available tools for the specified agent.</returns>
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
}
