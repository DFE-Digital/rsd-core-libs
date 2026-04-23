namespace GovUK.Dfe.CoreLibs.Security.EntraSso;

/// <summary>
/// Default values for Microsoft Entra ID SSO authentication
/// </summary>
public static class EntraSsoDefaults
{
    /// <summary>
    /// The authentication scheme name for Entra SSO interactive OIDC login
    /// </summary>
    public const string AuthenticationScheme = "EntraSso";

    /// <summary>
    /// The authentication scheme name for Entra SSO JWT bearer validation (API)
    /// </summary>
    public const string BearerScheme = "EntraSsoBearer";

    /// <summary>
    /// The default configuration section name
    /// </summary>
    public const string ConfigurationSection = "EntraSso";
}
