namespace GovUK.Dfe.CoreLibs.Email.Models;

/// <summary>
/// Represents email template information
/// </summary>
public class EmailTemplate
{
    /// <summary>
    /// Template identifier
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Template name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Template version
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// URI for accessing template details (if supported)
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// Template subject
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Template body
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Template type (e.g., email, sms, letter)
    /// </summary>
    public string? Type { get; set; }
}
