namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// WAF-safe body for cloning a tenant.
/// Hostname, frontend origin, and secrets are packed into <see cref="PayloadJson"/>
/// (UTF-8 JSON, then Base64) so Application Gateway WAF does not inspect
/// <c>https://...</c> values as ARGS (rule 931130 RFI).
/// </summary>
/// <param name="PayloadJson">
/// Base64-encoded UTF-8 JSON matching <see cref="CloneTenantSecretsPayload"/>.
/// </param>
public sealed record CloneTenantRequest(
    Guid NewTenantId,
    string NewTenantName,
    string PayloadJson);

/// <summary>
/// Decoded contents of <see cref="CloneTenantRequest.PayloadJson"/>.
/// </summary>
public sealed class CloneTenantSecretsPayload
{
    public string Hostname { get; set; } = string.Empty;

    public string FrontendOrigin { get; set; } = string.Empty;

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
