using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Context;

public sealed class RelativeScoreRelevanceFilterTests
{
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
}
