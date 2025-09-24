namespace GovUK.Dfe.CoreLibs.Email.Models;

/// <summary>
/// Represents email content information
/// </summary>
public class EmailContent
{
    /// <summary>
    /// Email subject
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Email body content
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// From email address
    /// </summary>
    public string? FromEmail { get; set; }

    /// <summary>
    /// To email address
    /// </summary>
    public string? ToEmail { get; set; }
}
