namespace GovUK.Dfe.CoreLibs.Email.Settings;

/// <summary>
/// Configuration options for GOV.UK Notify email provider
/// </summary>
public class GovUkNotifyOptions
{
    /// <summary>
    /// GOV.UK Notify API key
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL for GOV.UK Notify API (optional, uses default if not specified)
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// HTTP client timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to use proxy (if configured in HttpClient)
    /// </summary>
    public bool UseProxy { get; set; } = false;

    /// <summary>
    /// Maximum file size for attachments in bytes (default 2MB)
    /// </summary>
    public long MaxAttachmentSize { get; set; } = 2 * 1024 * 1024;

    /// <summary>
    /// Allowed file types for attachments
    /// </summary>
    public List<string> AllowedAttachmentTypes { get; set; } = new()
    {
        ".pdf", ".csv", ".txt", ".doc", ".docx", ".xls", ".xlsx", ".rtf", ".odt", ".ods", ".odp"
    };
}
