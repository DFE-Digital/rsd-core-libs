namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// Result of granting Template Read/Write access to every active member of a tenant.
/// </summary>
public sealed record GrantTemplateAccessToAllUsersResponse(
    Guid TemplateId,
    int TotalUsers,
    int UsersGranted,
    int UsersAlreadyHadAccess);
