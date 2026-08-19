namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

public sealed record TenantHealthCheckDto(
    string Code,
    string Label,
    string Status,
    string Detail);

/// <summary>
/// Tenant-focused health summary for SuperAdmin Tenant Settings.
/// </summary>
public sealed record TenantHealthDto(
    Guid TenantId,
    string TenantName,
    string OverallStatus,
    IReadOnlyList<TenantHealthCheckDto> Checks,
    TenantEffectiveConfigurationDto EffectiveConfiguration);
