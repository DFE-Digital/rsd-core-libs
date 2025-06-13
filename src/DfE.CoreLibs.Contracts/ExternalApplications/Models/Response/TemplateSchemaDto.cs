namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;

public class TemplateSchemaDto
{
    public required Guid TemplateId { get; set; }
    public required string VersionNumber { get; set; }
    public required string JsonSchema { get; set; }
}