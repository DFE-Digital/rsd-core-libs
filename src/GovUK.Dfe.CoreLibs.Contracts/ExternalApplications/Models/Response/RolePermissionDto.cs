using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// A permission grant attached to a role.
/// </summary>
public class RolePermissionDto
{
    public Guid RolePermissionId { get; set; }
    public ResourceType ResourceType { get; set; }
    public string ResourceKey { get; set; } = null!;
    public AccessType AccessType { get; set; }
}
