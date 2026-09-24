using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Context;

public sealed class RelativeScoreRelevanceFilterTests
{
    [Fact]
    public void Filter_KeepsOnlyResultsWithinHalfOfTopScore()
    {
        var sut = new RelativeScoreRelevanceFilter();

        var results = sut.Filter(
        [
            new SearchResultItem("strong match", Score: 1.0),
            new SearchResultItem("borderline match", Score: 0.5),
            new SearchResultItem("weak match", Score: 0.1)
        ]);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, x => x.Content == "strong match");
        Assert.Contains(results, x => x.Content == "borderline match");
        Assert.DoesNotContain(results, x => x.Content == "weak match");
    }

    [Fact]
    public void Filter_FallsBackToUnfiltered_WhenNoResultHasAUsableScore()
    {
        var sut = new RelativeScoreRelevanceFilter();

        var input = new[]
        {
            new SearchResultItem("result one", Score: null),
            new SearchResultItem("result two", Score: null)
        };

        var results = sut.Filter(input);

        Assert.Equal(input, results);
    }

    [Fact]
    public void Filter_ReturnsEmpty_WhenGivenNoResults()
    {
        var sut = new RelativeScoreRelevanceFilter();

        var results = sut.Filter([]);

        Assert.Empty(results);
    }
}
