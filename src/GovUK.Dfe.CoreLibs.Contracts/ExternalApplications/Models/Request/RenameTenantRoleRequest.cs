namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Renames a custom tenant role.
/// </summary>
public class RenameTenantRoleRequest
{
    public string Name { get; set; } = null!;
}
