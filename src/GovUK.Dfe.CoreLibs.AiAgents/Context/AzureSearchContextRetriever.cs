using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.Context.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GovUK.Dfe.CoreLibs.AiAgents.Context;

/// <param name="clients">One search client per index, keyed by the scope name callers pass.</param>
/// <param name="relevanceFilter">Drops weak matches before they reach the prompt.</param>
/// <param name="contentFields">
/// The fields to use as evidence for each index, keyed by scope, in order. An index with no entry
/// uses every non-empty string field.
/// </param>
public sealed class AzureSearchContextRetriever(IReadOnlyDictionary<string, SearchClient> clients, IRelevanceFilter relevanceFilter,
    ILogger<AzureSearchContextRetriever>? logger = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? contentFields = null)
    : IContextRetriever
{
    private readonly ILogger<AzureSearchContextRetriever> _logger = logger ?? NullLogger<AzureSearchContextRetriever>.Instance;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _contentFields =
        contentFields ?? new Dictionary<string, IReadOnlyList<string>>();

    public async Task<ContextResult> GetContextAsync(string scope, string query, int size = 10, string? filter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        if (!clients.TryGetValue(scope, out var client))
        {
            throw new InvalidOperationException(string.Format(ErrorMessages.NoAzureSearchClientConfigured, scope));
        }

        var relevant = relevanceFilter.Filter(await SearchAsync(client, scope, query, size, filter, cancellationToken).ConfigureAwait(false));
        if (relevant.Count == 0)
        {
            return new ContextResult(string.Format(ErrorMessages.NoAzureSearchInformationFound, scope), HasEvidence: false);
        }

        return new ContextResult(string.Join(Environment.NewLine + Environment.NewLine, relevant.Select((item, i)
            => $"--- {scope} Evidence {i + 1} ---\n{item.Content}")), HasEvidence: true);
    }

    private async Task<IReadOnlyList<SearchResultItem>> SearchAsync(SearchClient client, string scope, string query, int size, string? filter,
        CancellationToken cancellationToken)
    {
        var fields = FieldsFor(scope);
        var searchOptions = new SearchOptions { Size = size, Filter = filter };
        foreach (var field in fields)
        {
            searchOptions.Select.Add(field);
        }

        var results = new List<SearchResultItem>();

        try
        {
            SearchResults<SearchDocument> response = await client.SearchAsync<SearchDocument>(query, searchOptions, cancellationToken)
                .ConfigureAwait(false);

            await foreach (var result in response.GetResultsAsync().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var content = ExtractContent(result.Document, fields);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    results.Add(new SearchResultItem(content, result.Score));
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Azure Search query against {Scope} failed", scope);
            throw new InvalidOperationException(string.Format(ErrorMessages.AzureSearchQueryFailed, scope), ex);
        }

        return results;
    }

    private IReadOnlyList<string> FieldsFor(string scope)
        => _contentFields.TryGetValue(scope, out var fields) ? fields : [];

    private static string ExtractContent(SearchDocument document, IReadOnlyList<string> fields)
    {
        var values = fields.Count > 0
            ? fields.Select(field => document.TryGetValue(field, out var value) ? value : null)
            : document.Select(field => field.Value);

        return string.Join(" ", values.OfType<string>().Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
