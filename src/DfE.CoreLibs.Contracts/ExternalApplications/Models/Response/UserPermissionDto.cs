using DfE.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;

public class UserPermissionDto
{
    public Guid? ApplicationId { get; set; } = null;
    public string ResourceKey { get; set; } = string.Empty;
    public required ResourceType ResourceType { get; set; }
    public required AccessType AccessType { get; set; }
}