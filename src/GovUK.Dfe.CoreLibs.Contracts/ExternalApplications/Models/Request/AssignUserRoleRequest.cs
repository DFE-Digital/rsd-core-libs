namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Client contract for assigning a predefined role to a user.
/// </summary>
public class AssignUserRoleRequest
{
    public string Email { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Role { get; set; } = null!;

    public IReadOnlyCollection<Guid>? TemplateIds { get; set; }
}
