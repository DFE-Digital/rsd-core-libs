namespace GovUK.Dfe.CoreLibs.Email.Settings;

/// <summary>
/// Configuration options for SMTP email provider
/// </summary>
public class SmtpOptions
{
    /// <summary>
    /// SMTP server host
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// SMTP server port
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Username for SMTP authentication
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password for SMTP authentication
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Whether to use SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Whether to use StartTLS
    /// </summary>
    public bool UseStartTls { get; set; } = true;

    /// <summary>
    /// Connection timeout in milliseconds
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;
}
