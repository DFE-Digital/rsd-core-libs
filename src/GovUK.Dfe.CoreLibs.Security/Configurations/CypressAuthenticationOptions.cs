namespace GovUK.Dfe.CoreLibs.Security.Configurations;

/// <summary>
/// Configuration options for cypress authentication
/// </summary>
public class CypressAuthenticationOptions
{
    public const string SectionName = "CypressAuthentication";

    /// <summary>
    /// Whether cypress is allowed to enable Test authentication
    /// </summary>
    public bool AllowToggle { get; set; } = false;

    /// <summary>
    /// The cypress secret key
    /// </summary>
    public string Secret { get; set; } = string.Empty;
}