using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.WebSearch;

/// <summary>Gives an agent Foundry's web search, biased towards <paramref name="location"/> (default: the UK).</summary>
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
