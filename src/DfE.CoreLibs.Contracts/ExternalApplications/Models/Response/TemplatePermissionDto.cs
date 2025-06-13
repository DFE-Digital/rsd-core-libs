using DfE.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public class TemplatePermissionDto
    {
        public Guid? TemplatePermissionId { get; set; } = null;
        public required Guid TemplateId { get; set; }
        public required Guid UserId { get; set; }
        public required AccessType AccessType { get; set; }
    }
}
