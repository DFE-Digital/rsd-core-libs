namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// WAF-safe body for cloning a tenant. Secrets are not sent as discrete JSON properties;
/// they are packed into <see cref="PayloadJson"/> (UTF-8 JSON, then Base64) so Front Door
/// does not inspect secret field names or values.
/// </summary>
/// <param name="PayloadJson">
/// Base64-encoded UTF-8 JSON matching <see cref="CloneTenantSecretsPayload"/>.
/// </param>
public sealed record CloneTenantRequest(
    Guid NewTenantId,
    string NewTenantName,
    string Hostname,
    string FrontendOrigin,
    string PayloadJson);

/// <summary>
/// Decoded contents of <see cref="CloneTenantRequest.PayloadJson"/>.
/// </summary>
public sealed class CloneTenantSecretsPayload
{
    public string AuthorizationApiSecretKey { get; set; } = string.Empty;

    public string InternalServiceAuthSecretKey { get; set; } = string.Empty;

    public List<CloneTenantServiceApiKeyPayload> InternalServiceAuthServiceApiKeys { get; set; } = [];
}

/// <summary>
/// One InternalServiceAuth Services[] entry inside <see cref="CloneTenantSecretsPayload"/>.
/// </summary>
public sealed class CloneTenantServiceApiKeyPayload
{
    public string Email { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
