using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.WebSearch;

/// <summary>
/// Provides a web search tool for agents, allowing them to perform web searches based on a specified location.
/// </summary>
/// <param name="location"></param>
public sealed class WebSearchToolProvider(WebSearchLocation? location = null) : IAgentToolProvider
{
    private readonly WebSearchLocation _location = location ?? WebSearchLocation.UnitedKingdom;

    public Task<IReadOnlyList<ResponseTool>> GetToolsAsync(CancellationToken cancellationToken = default)
    {
        var tool = ResponseTool.CreateWebSearchTool(userLocation: WebSearchToolLocation.CreateApproximateLocation(
            country: _location.Country, region: _location.Region, city: _location.City));

        return Task.FromResult<IReadOnlyList<ResponseTool>>([tool]);
    }
}
