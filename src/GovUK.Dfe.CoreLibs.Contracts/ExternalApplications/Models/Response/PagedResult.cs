using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
public class PagedResult<T>
{
    [JsonPropertyName("items")]
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }

    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; init; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }
    
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; init; }
}
