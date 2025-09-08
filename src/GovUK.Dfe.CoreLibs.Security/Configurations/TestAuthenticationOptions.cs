namespace GovUK.Dfe.CoreLibs.Security.Configurations;

/// <summary>
/// Configuration options for test authentication token validation
/// </summary>
public class TestAuthenticationOptions
{
    public const string SectionName = "TestAuthentication";

    /// <summary>
    /// Whether test authentication is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The JWT signing key used to validate test tokens
    /// </summary>
    public string JwtSigningKey { get; set; } = string.Empty;

    /// <summary>
    /// The expected issuer of test tokens
    /// </summary>
    public string JwtIssuer { get; set; } = string.Empty;

    /// <summary>
    /// The expected audience of test tokens
    /// </summary>
    public string JwtAudience { get; set; } = string.Empty;

    /// <summary>
    /// Whether to validate the token lifetime (default: true)
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Whether to validate the issuer (default: true)
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate the audience (default: true)
    /// </summary>
    public bool ValidateAudience { get; set; } = true;
}
