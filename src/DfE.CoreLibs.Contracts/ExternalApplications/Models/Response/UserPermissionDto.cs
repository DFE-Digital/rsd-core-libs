using DfE.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public class UserPermissionDto
    {
        public Guid ApplicationId { get; set; }
        public string ResourceKey { get; set; } = string.Empty;
        public AccessType AccessType { get; set; }
    }
}