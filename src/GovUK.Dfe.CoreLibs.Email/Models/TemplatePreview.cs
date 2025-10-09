namespace GovUK.Dfe.CoreLibs.Email.Models;

/// <summary>
/// Represents a preview of a template with personalization applied
/// </summary>
public class TemplatePreview
{
    /// <summary>
    /// Template identifier
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Template version
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Preview subject with personalization applied
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Preview body with personalization applied
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Template type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// HTML version of the body (if available)
    /// </summary>
    public string? Html { get; set; }
}
