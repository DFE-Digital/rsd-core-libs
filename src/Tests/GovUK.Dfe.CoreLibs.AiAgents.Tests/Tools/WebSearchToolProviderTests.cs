using GovUK.Dfe.CoreLibs.AiAgents.Tools.WebSearch;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using OpenAI.Responses;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Tools;

public sealed class WebSearchToolProviderTests
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetToolsAsync_DefaultsToTheUnitedKingdom_WhenNoLocationIsGiven()
    {
        var sut = new WebSearchToolProvider();

        var tools = await sut.GetToolsAsync(cancellationToken);

        var tool = Assert.IsType<WebSearchTool>(Assert.Single(tools));
        Assert.NotNull(tool.UserLocation);
    }

    [Fact]
    public async Task GetToolsAsync_SetsTheApproximateLocation_WhenGiven()
    {
        var sut = new WebSearchToolProvider(new WebSearchLocation(Country: "US", Region: "California", City: "San Francisco"));

        var tools = await sut.GetToolsAsync(cancellationToken);

        var tool = Assert.IsType<WebSearchTool>(Assert.Single(tools));
        Assert.NotNull(tool.UserLocation);
    }
}
