using System.Text.Json.Serialization;

namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;

public class TemplateSchemaDto
{
    [JsonPropertyName("templateId")]
    public required Guid TemplateId { get; set; }

    [JsonPropertyName("templateVersionId")]
    public required Guid TemplateVersionId { get; set; }

    [JsonPropertyName("versionNumber")]
    public required string VersionNumber { get; set; }

    [JsonPropertyName("jsonSchema")]
    public required string JsonSchema { get; set; }
}
