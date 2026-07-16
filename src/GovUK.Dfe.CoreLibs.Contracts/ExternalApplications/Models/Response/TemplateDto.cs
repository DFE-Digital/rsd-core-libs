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

    /// <summary>
    /// When <c>true</c>, end users with permission can access this template.
    /// Admins receive all catalogue templates and can use this flag for publish/preview UX.
    /// </summary>
    [JsonPropertyName("isLive")]
    public bool IsLive { get; set; }
}
