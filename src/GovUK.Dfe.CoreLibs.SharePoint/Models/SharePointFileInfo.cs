namespace GovUK.Dfe.CoreLibs.SharePoint.Models;

/// <summary>
/// Metadata for a file in a SharePoint document library.
/// </summary>
public class SharePointFileInfo
{
    /// <summary>
    /// Drive item ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// File name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Last modified timestamp (UTC), if available.
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// Web URL of the file, if available.
    /// </summary>
    public string? WebUrl { get; set; }
}
