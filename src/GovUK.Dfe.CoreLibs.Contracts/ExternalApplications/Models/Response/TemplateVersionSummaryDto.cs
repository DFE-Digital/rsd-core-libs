using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// Lightweight template version metadata for version pickers (no schema body).
/// </summary>
public class TemplateVersionSummaryDto
{
    [JsonPropertyName("templateId")]
    public required Guid TemplateId { get; set; }

    [JsonPropertyName("templateVersionId")]
    public required Guid TemplateVersionId { get; set; }

    [JsonPropertyName("versionNumber")]
    public required string VersionNumber { get; set; }

    [JsonPropertyName("createdOn")]
    public required DateTime CreatedOn { get; set; }
}
