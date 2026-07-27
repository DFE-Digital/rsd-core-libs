namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Creates a custom tenant role (non-system).
/// </summary>
public class CreateTenantRoleRequest
{
    public string Name { get; set; } = null!;
}
