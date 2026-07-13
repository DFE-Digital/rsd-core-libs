namespace GovUK.Dfe.CoreLibs.SharePoint.Settings;

/// <summary>
/// Configuration options for SharePoint access via Microsoft Graph.
/// </summary>
public class SharePointOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "SharePoint";

    /// <summary>
    /// Azure AD / Entra tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Application (client) ID of the registered app.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret for app-only authentication. Required when certificate settings are not provided.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Path to a PFX certificate file for app-only authentication. Required when <see cref="ClientSecret"/> is not provided.
    /// </summary>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// Optional password for the certificate at <see cref="CertificatePath"/>.
    /// </summary>
    public string CertificatePassword { get; set; } = string.Empty;

    /// <summary>
    /// SharePoint site ID. Required when <see cref="SiteHostname"/> and <see cref="SitePath"/> are not provided.
    /// </summary>
    public string SiteId { get; set; } = string.Empty;

    /// <summary>
    /// SharePoint hostname (e.g. contoso.sharepoint.com). Used with <see cref="SitePath"/> when <see cref="SiteId"/> is not set.
    /// </summary>
    public string SiteHostname { get; set; } = string.Empty;

    /// <summary>
    /// Site-relative path (e.g. /sites/MySite). Used with <see cref="SiteHostname"/> when <see cref="SiteId"/> is not set.
    /// </summary>
    public string SitePath { get; set; } = string.Empty;

    /// <summary>
    /// Document library drive ID. Required when <see cref="LibraryName"/> is not provided.
    /// </summary>
    public string DriveId { get; set; } = string.Empty;

    /// <summary>
    /// Document library display name (e.g. Documents). Used to resolve the drive when <see cref="DriveId"/> is not set.
    /// </summary>
    public string LibraryName { get; set; } = "Documents";
}
