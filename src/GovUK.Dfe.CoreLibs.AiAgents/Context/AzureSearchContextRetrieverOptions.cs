namespace GovUK.Dfe.CoreLibs.AiAgents.Context;

public sealed class AzureSearchContextRetrieverOptions
{
    public required string Endpoint { get; init; }

    /// <summary>The Entra ID tenant, for the client-secret flow. Not needed when a credential is passed in (as <c>AddAiAgents</c> does).</summary>
    public string? TenantId { get; init; }

    /// <summary>The Entra ID client, for the client-secret flow. Not needed when a credential is passed in (as <c>AddAiAgents</c> does).</summary>
    public string? ClientId { get; init; }

    /// <summary>The client secret, for the client-secret flow. Not needed when a credential is passed in (as <c>AddAiAgents</c> does).</summary>
    public string? ClientSecret { get; init; }

    /// <summary>The indexes agents can search, each with the fields to use as evidence.</summary>
    public required IReadOnlyList<AzureSearchIndexOptions> Indexes { get; init; }

    public double MinimumRelevanceFilter { get; init; } = 0.5;

    public int MaxRetryAttempts { get; init; } = 3;

    internal bool HasClientSecretCredential
        => !string.IsNullOrWhiteSpace(TenantId) && !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

    /// <summary>Every index has a name, and no name appears twice.</summary>
    internal bool IndexesAreValid
        => Indexes.Count > 0
           && Indexes.All(index => !string.IsNullOrWhiteSpace(index.Name))
           && Indexes.Select(index => index.Name).Distinct(StringComparer.Ordinal).Count() == Indexes.Count;

    /// <summary>Redacts <see cref="ClientSecret"/> so this never leaks via logging or exception messages.</summary>
    public override string ToString()
        => $"{{ Endpoint = {Endpoint}, TenantId = {TenantId}, ClientId = {ClientId}, ClientSecret = [REDACTED], " +
           $"Indexes = [{string.Join("; ", Indexes.Select(index => $"{index.Name}: {string.Join(", ", index.ContentFields)}"))}] }}";
}

/// <summary>
/// One Azure AI Search index agents can use.
/// </summary>
public sealed class AzureSearchIndexOptions
{
    /// <summary>The index name, also the <c>scope</c> passed to <c>GetContextAsync</c>.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// The fields sent to the model as evidence, in this order. Also sent as the search <c>$select</c>,
    /// so list only fields the index has. Empty uses every non-empty string field - which can include
    /// ids, URLs and metadata, so listing fields is recommended.
    /// </summary>
    public IReadOnlyList<string> ContentFields { get; init; } = [];
}
