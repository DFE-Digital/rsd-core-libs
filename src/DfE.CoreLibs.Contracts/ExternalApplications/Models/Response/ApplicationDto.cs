using DfE.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed record ApplicationDto
    {
        public Guid ApplicationId { get; set; }
        public string ApplicationReference { get; set; } = string.Empty;
        public Guid TemplateVersionId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public ApplicationStatus? Status { get; set; } = null;
        public DateTime DateCreated { get; set; }
        public DateTime? DateSubmitted { get; set; }
        public ApplicationResponseDetailsDto? LatestResponse { get; init; } = null;
    }
}
