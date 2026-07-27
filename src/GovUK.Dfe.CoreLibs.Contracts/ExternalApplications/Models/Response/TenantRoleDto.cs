namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// Tenant-scoped role summary.
/// </summary>
public class TenantRoleDto
{
    public Guid RoleId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsSystem { get; set; }
}
