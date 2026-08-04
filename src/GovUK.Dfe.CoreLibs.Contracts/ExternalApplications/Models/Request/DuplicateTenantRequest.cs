namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Creates a new tenant by copying TenantConfig settings from an existing tenant.
/// Hostname and frontend origin must be unique. Principals are not copied.
/// </summary>
public sealed record DuplicateTenantRequest(
    Guid NewTenantId,
    string NewTenantName,
    string Hostname,
    string FrontendOrigin);
