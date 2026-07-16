using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// Summary of a template available within a tenant.
/// </summary>
public class TemplateDto
{
    [JsonPropertyName("templateId")]
    public required Guid TemplateId { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("createdOn")]
    public required DateTime CreatedOn { get; set; }

    [JsonPropertyName("latestVersionNumber")]
    public string? LatestVersionNumber { get; set; }
}
