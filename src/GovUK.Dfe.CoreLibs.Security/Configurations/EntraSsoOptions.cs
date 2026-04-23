namespace GovUK.Dfe.CoreLibs.Security.Configurations;

/// <summary>
/// Configuration options for Microsoft Entra ID (Azure AD) SSO authentication.
/// Supports both interactive OIDC sign-in (Web) and JWT bearer validation (API).
/// </summary>
public class EntraSsoOptions
{
    public const string SectionName = "EntraSso";

    /// <summary>
    /// Whether Entra SSO authentication is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Azure AD instance URL (e.g. "https://login.microsoftonline.com/")
    /// </summary>
    public string Instance { get; set; } = "https://login.microsoftonline.com/";

    /// <summary>
    /// Azure AD / Entra tenant ID (GUID or domain name, e.g. "contoso.onmicrosoft.com")
    /// Use "common" for multi-tenant, "organizations" for any org, or a specific tenant ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Application (client) ID registered in Entra ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret for confidential client authentication
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The callback path where Entra returns after authentication (default: /signin-entra)
    /// </summary>
    public string CallbackPath { get; set; } = "/signin-entra";

    /// <summary>
    /// The path for signed-out callback (default: /signout-callback-entra)
    /// </summary>
    public string SignedOutCallbackPath { get; set; } = "/signout-callback-entra";

    /// <summary>
    /// OAuth 2.0 response type (default: "code" for authorization code flow)
    /// </summary>
    public string ResponseType { get; set; } = "code";

    /// <summary>
    /// Whether to save tokens in the authentication properties
    /// </summary>
    public bool SaveTokens { get; set; } = true;

    /// <summary>
    /// Whether to get claims from the UserInfo endpoint
    /// </summary>
    public bool GetClaimsFromUserInfoEndpoint { get; set; } = true;

    /// <summary>
    /// Whether to require HTTPS metadata
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Whether to use the token lifetime for the cookie
    /// </summary>
    public bool UseTokenLifetime { get; set; } = true;

    /// <summary>
    /// The claim type used for the user's display name
    /// </summary>
    public string NameClaimType { get; set; } = "preferred_username";

    /// <summary>
    /// OAuth scopes to request
    /// </summary>
    public IList<string> Scopes { get; set; } = new List<string> { "openid", "profile", "email" };

    /// <summary>
    /// API audience for JWT bearer token validation (typically "api://{ClientId}")
    /// Used by API projects for bearer token validation
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Computed authority URL from Instance and TenantId
    /// </summary>
    public string Authority => $"{Instance.TrimEnd('/')}/{TenantId}/v2.0";
}
