using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// A single permission grant to attach to a role.
/// </summary>
public class RolePermissionGrantDto
{
    public ResourceType ResourceType { get; set; }
    public string ResourceKey { get; set; } = null!;
    public AccessType AccessType { get; set; }
}

/// <summary>
/// Replaces the full set of permissions on a tenant role.
/// </summary>
public class SetRolePermissionsRequest
{
    public IReadOnlyCollection<RolePermissionGrantDto> Permissions { get; set; }
        = Array.Empty<RolePermissionGrantDto>();
}
