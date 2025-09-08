namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request
{
    public class CreateApplicationRequest
    {
        public required Guid TemplateId { get; set; }
        public required string InitialResponseBody { get; set; } = string.Empty;
    }
}
