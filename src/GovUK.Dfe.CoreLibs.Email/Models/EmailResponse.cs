namespace GovUK.Dfe.CoreLibs.Email.Models;

/// <summary>
/// Represents the response from sending an email
/// </summary>
public class EmailResponse
{
    /// <summary>
    /// Unique identifier for the sent email
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Reference provided when sending the email
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// URI for accessing the email details (if supported by provider)
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// Current status of the email
    /// </summary>
    public EmailStatus Status { get; set; }

    /// <summary>
    /// Template information (if template was used)
    /// </summary>
    public EmailTemplate? Template { get; set; }

    /// <summary>
    /// Email content information
    /// </summary>
    public EmailContent? Content { get; set; }

    /// <summary>
    /// Timestamp when the email was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the email was sent (if available)
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Timestamp when the status was last updated
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Provider-specific metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// List of recipient email addresses (for multi-recipient emails)
    /// </summary>
    public List<string>? Recipients { get; set; }

    /// <summary>
    /// Individual responses for each recipient (when supported by provider)
    /// </summary>
    public List<EmailResponse>? RecipientResponses { get; set; }
}
