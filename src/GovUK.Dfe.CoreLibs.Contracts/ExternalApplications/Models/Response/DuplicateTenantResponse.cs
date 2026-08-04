namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// Result of duplicating a tenant's TenantConfig (settings, hostname, origin).
/// </summary>
public sealed record DuplicateTenantResponse(
    Guid SourceTenantId,
    Guid NewTenantId,
    string NewTenantName,
    string Hostname,
    string FrontendOrigin,
    int SettingsCopied,
    string Message);
