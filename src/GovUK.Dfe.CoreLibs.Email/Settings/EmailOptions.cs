namespace GovUK.Dfe.CoreLibs.Email.Settings;

/// <summary>
/// Configuration options for the email service
/// </summary>
public class EmailOptions
{
    /// <summary>
    /// The email provider to use (e.g., "GovUkNotify", "SendGrid", "Smtp")
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Default from email address
    /// </summary>
    public string? DefaultFromEmail { get; set; }

    /// <summary>
    /// Default from name
    /// </summary>
    public string? DefaultFromName { get; set; }

    /// <summary>
    /// Whether to enable email validation
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// Timeout for email operations in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts for failed operations
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Whether to throw exceptions on validation errors
    /// </summary>
    public bool ThrowOnValidationError { get; set; } = true;

    /// <summary>
    /// GOV.UK Notify specific settings
    /// </summary>
    public GovUkNotifyOptions GovUkNotify { get; set; } = new();

    /// <summary>
    /// SMTP specific settings
    /// </summary>
    public SmtpOptions Smtp { get; set; } = new();
}
