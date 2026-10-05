namespace GovUK.Dfe.CoreLibs.AiAgents.Context;

/// <summary>The <c>AiAgents:Search</c> section.</summary>
public sealed class AzureSearchContextRetrieverOptions
{
    public required string Endpoint { get; init; }

    public required IReadOnlyList<AzureSearchIndexOptions> Indexes { get; init; }

    /// <summary>Results scoring below this fraction of the top score are dropped.</summary>
    public double MinimumRelevanceFilter { get; init; } = 0.5;

    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>Every index has a name, and no name appears twice.</summary>
    internal bool IndexesAreValid
        => Indexes.Count > 0
           && Indexes.All(index => !string.IsNullOrWhiteSpace(index.Name))
           && Indexes.Select(index => index.Name).Distinct(StringComparer.Ordinal).Count() == Indexes.Count;
}

/// <summary>One index agents can search.</summary>
public sealed class AzureSearchIndexOptions
{
    /// <summary>The index name, also the <c>scope</c> passed to <c>GetContextAsync</c>.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// Fields sent to the model, in order (also the search <c>$select</c>). Empty sends every string field,
    /// including ids and URLs.
    /// </summary>
    public IReadOnlyList<string> ContentFields { get; init; } = [];
}
