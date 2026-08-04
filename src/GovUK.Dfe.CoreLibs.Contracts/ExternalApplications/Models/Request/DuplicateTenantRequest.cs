namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// One InternalServiceAuth Services[] entry ApiKey to apply when duplicating a tenant.
/// </summary>
public sealed record DuplicateTenantServiceApiKey(string Email, string ApiKey);

/// <summary>
/// Creates a new tenant by copying TenantConfig settings from an existing tenant.
/// Hostname and frontend origin must be unique. Principals are not copied.
/// Authorization and InternalServiceAuth secrets are replaced with the supplied values
/// (InternalServiceAuth is written identically to Api and Web targets).
/// </summary>
public sealed record DuplicateTenantRequest(
    Guid NewTenantId,
    string NewTenantName,
    string Hostname,
    string FrontendOrigin,
    string AuthorizationApiSecretKey,
    string InternalServiceAuthSecretKey,
    IReadOnlyList<DuplicateTenantServiceApiKey> InternalServiceAuthServiceApiKeys);
