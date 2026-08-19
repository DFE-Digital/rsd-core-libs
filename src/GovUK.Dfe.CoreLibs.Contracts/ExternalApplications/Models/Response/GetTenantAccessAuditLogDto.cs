namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>One tenant user/role access change.</summary>
public sealed record TenantAccessAuditEntryDto(
    Guid Id,
    Guid TenantId,
    Guid? SubjectUserId,
    string SubjectEmail,
    string Action,
    string? RoleName,
    Guid? ActorUserId,
    string ActorEmail,
    string? Details,
    DateTime OccurredAtUtc);

/// <summary>Recent tenant access audit entries.</summary>
public sealed record GetTenantAccessAuditLogDto(
    Guid TenantId,
    IReadOnlyCollection<TenantAccessAuditEntryDto> Entries);
