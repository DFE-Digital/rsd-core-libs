namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

public sealed record TenantSettingAuditEntryDto(
    Guid Id,
    string Category,
    string Target,
    string Action,
    string ActorEmail,
    DateTime ChangedAtUtc,
    bool WasSecret);

public sealed record GetTenantSettingAuditLogDto(
    Guid TenantId,
    IReadOnlyList<TenantSettingAuditEntryDto> Entries);
