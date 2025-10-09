namespace GovUK.Dfe.CoreLibs.Email.Models;

/// <summary>
/// Represents an email message to be sent
/// </summary>
public class EmailMessage
{
    /// <summary>
    /// Primary recipient email address (for backward compatibility)
    /// </summary>
    public string? ToEmail { get; set; }

    /// <summary>
    /// Multiple recipient email addresses (if sending to multiple recipients)
    /// </summary>
    public List<string>? ToEmails { get; set; }

    /// <summary>
    /// Email subject (used for non-template emails)
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Email body content (used for non-template emails)
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Template ID for template-based emails
    /// </summary>
    public string? TemplateId { get; set; }

    /// <summary>
    /// Personalization data for template placeholders
    /// </summary>
    public Dictionary<string, object>? Personalization { get; set; }

    /// <summary>
    /// File attachments
    /// </summary>
    public List<EmailAttachment>? Attachments { get; set; }

    /// <summary>
    /// Optional reference identifier for tracking
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// Optional email reply-to address
    /// </summary>
    public string? ReplyToEmail { get; set; }

    /// <summary>
    /// Gets all recipient email addresses (combines ToEmail and ToEmails)
    /// </summary>
    /// <returns>List of all recipient email addresses</returns>
    public List<string> GetAllRecipients()
    {
        var recipients = new List<string>();

        // Add single recipient if specified
        if (!string.IsNullOrWhiteSpace(ToEmail))
        {
            recipients.Add(ToEmail);
        }

        // Add multiple recipients if specified
        if (ToEmails?.Any() == true)
        {
            recipients.AddRange(ToEmails.Where(email => !string.IsNullOrWhiteSpace(email)));
        }

        return recipients.Distinct().ToList(); // Remove duplicates
    }

    /// <summary>
    /// Gets the primary recipient email address
    /// </summary>
    /// <returns>The first available email address</returns>
    public string? GetPrimaryRecipient()
    {
        if (!string.IsNullOrWhiteSpace(ToEmail))
            return ToEmail;

        return ToEmails?.FirstOrDefault(email => !string.IsNullOrWhiteSpace(email));
    }
}
