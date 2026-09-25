using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using GovUK.Dfe.CoreLibs.AiAgents.Context;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Context;

public sealed class AzureSearchContextRetrieverTests
{
    private const string Scope = "establishment";

    private readonly CancellationToken cancellationToken = default;
    private readonly SearchClient _client = Substitute.For<SearchClient>();
    private readonly RelativeScoreRelevanceFilter _relevanceFilter = new();

    private AzureSearchContextRetriever CreateSut()
        => new(new Dictionary<string, SearchClient> { [Scope] = _client }, _relevanceFilter);

    private void SetUpSearchResults(params (string content, double? score)[] items)
    {
        var documents = items.Select(item =>
        {
            var document = new SearchDocument { ["content"] = item.content };
            return SearchModelFactory.SearchResult(document, item.score, highlights: null);
        });

        var results = SearchModelFactory.SearchResults(
            values: documents, totalCount: items.Length, facets: null, coverage: null, rawResponse: null!);

        _client.SearchAsync<SearchDocument>(Arg.Any<string>(), Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(results, null!));
    }

    [Fact]
    public async Task GetContextAsync_ThrowsInvalidOperationException_WhenScopeNotConfigured()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.GetContextAsync("unknown-scope", "query", cancellationToken: cancellationToken));
    }

    [Theory]
    [InlineData("", "query")]
    [InlineData("  ", "query")]
    [InlineData(Scope, "")]
    [InlineData(Scope, "  ")]
    public async Task GetContextAsync_ThrowsArgumentException_WhenScopeOrQueryIsEmpty(string scope, string query)
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => sut.GetContextAsync(scope, query, cancellationToken: cancellationToken));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetContextAsync_ThrowsArgumentOutOfRangeException_WhenSizeIsNotPositive(int size)
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => sut.GetContextAsync(Scope, "query", size, cancellationToken));
    }

    [Fact]
    public async Task GetContextAsync_ReturnsNoEvidence_WhenSearchReturnsNoResults()
    {
        SetUpSearchResults();
        var sut = CreateSut();

        var result = await sut.GetContextAsync(Scope, "query", cancellationToken: cancellationToken);

        Assert.False(result.HasEvidence);
    }

    [Fact]
    public async Task GetContextAsync_FormatsOnlyResultsThatPassTheRealRelevanceFilter()
    {
        SetUpSearchResults(("strong match", 1.0), ("borderline match", 0.5), ("weak match", 0.1));
        var sut = CreateSut();

        var result = await sut.GetContextAsync(Scope, "query", cancellationToken: cancellationToken);

        Assert.True(result.HasEvidence);
        Assert.Contains("--- establishment Evidence 1 ---", result.Text);
        Assert.Contains("strong match", result.Text);
        Assert.Contains("--- establishment Evidence 2 ---", result.Text);
        Assert.Contains("borderline match", result.Text);
        Assert.DoesNotContain("weak match", result.Text);
    }

    [Fact]
    public async Task GetContextAsync_ExtractsOnlyStringFields_FromEachDocument()
    {
        var document = new SearchDocument
        {
            ["title"] = "A title",
            ["content"] = "Body text",
            ["pageCount"] = 42,
            ["publishedAt"] = null!,
        };
        var results = SearchModelFactory.SearchResults(
            values: [SearchModelFactory.SearchResult(document, 1.0, highlights: null)],
            totalCount: 1, facets: null, coverage: null, rawResponse: null!);
        _client.SearchAsync<SearchDocument>(Arg.Any<string>(), Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(results, null!));

        var sut = CreateSut();
        var result = await sut.GetContextAsync(Scope, "query", cancellationToken: cancellationToken);

        Assert.Contains("A title", result.Text);
        Assert.Contains("Body text", result.Text);
        Assert.DoesNotContain("42", result.Text);
    }

    [Fact]
    public async Task GetContextAsync_DropsResultsWithNoExtractableContent()
    {
        var emptyDocument = new SearchDocument { ["pageCount"] = 1 };
        var usableDocument = new SearchDocument { ["content"] = "usable content" };
        var results = SearchModelFactory.SearchResults(
            values:
            [
                SearchModelFactory.SearchResult(emptyDocument, 1.0, highlights: null),
                SearchModelFactory.SearchResult(usableDocument, 1.0, highlights: null),
            ],
            totalCount: 2, facets: null, coverage: null, rawResponse: null!);
        _client.SearchAsync<SearchDocument>(Arg.Any<string>(), Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(results, null!));

        var sut = CreateSut();
        var result = await sut.GetContextAsync(Scope, "query", cancellationToken: cancellationToken);

        Assert.Contains("--- establishment Evidence 1 ---", result.Text);
        Assert.Contains("usable content", result.Text);
        Assert.DoesNotContain("Evidence 2", result.Text);
    }

    [Fact]
    public async Task GetContextAsync_PassesRequestedSize_ToSearchOptions()
    {
        SetUpSearchResults(("some content", 1.0));
        var sut = CreateSut();

        await sut.GetContextAsync(Scope, "query", size: 7, cancellationToken: cancellationToken);

        await _client.Received(1).SearchAsync<SearchDocument>(
            "query", Arg.Is<SearchOptions>(o => o.Size == 7), cancellationToken);
    }

    [Fact]
    public async Task GetContextAsync_Rethrows_WhenSearchClientFails()
    {
        var failure = new InvalidOperationException("service unavailable");
        _client.SearchAsync<SearchDocument>(Arg.Any<string>(), Arg.Any<SearchOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Response<SearchResults<SearchDocument>>>(failure));

        var sut = CreateSut();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.GetContextAsync(Scope, "query", cancellationToken: cancellationToken));
        Assert.Same(failure, thrown.InnerException);
        Assert.Contains(Scope, thrown.Message, StringComparison.Ordinal);
    }
}
