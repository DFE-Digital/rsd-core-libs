namespace GovUK.Dfe.CoreLibs.Email.Models;

/// <summary>
/// Represents an email attachment
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// The filename of the attachment
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// The content of the file as a byte array
    /// </summary>
    public required byte[] Content { get; set; }

    /// <summary>
    /// The MIME content type of the file
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Whether the attachment should be treated as inline content
    /// </summary>
    public bool IsInline { get; set; } = false;

    /// <summary>
    /// Content ID for inline attachments
    /// </summary>
    public string? ContentId { get; set; }
}
