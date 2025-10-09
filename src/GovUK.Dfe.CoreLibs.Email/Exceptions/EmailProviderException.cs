namespace GovUK.Dfe.CoreLibs.Email.Exceptions;

/// <summary>
/// Exception thrown when an email provider encounters an error
/// </summary>
public class EmailProviderException : EmailException
{
    /// <summary>
    /// HTTP status code (if applicable)
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Provider name where the error occurred
    /// </summary>
    public string? ProviderName { get; }

    /// <summary>
    /// Creates a new EmailProviderException
    /// </summary>
    /// <param name="message">Provider error message</param>
    /// <param name="providerName">Name of the provider</param>
    public EmailProviderException(string message, string? providerName = null) : base(message, "ProviderError")
    {
        ProviderName = providerName;
    }

    /// <summary>
    /// Creates a new EmailProviderException with status code
    /// </summary>
    /// <param name="message">Provider error message</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="providerName">Name of the provider</param>
    public EmailProviderException(string message, int statusCode, string? providerName = null) : base(message, "ProviderError")
    {
        StatusCode = statusCode;
        ProviderName = providerName;
    }

    /// <summary>
    /// Creates a new EmailProviderException with inner exception
    /// </summary>
    /// <param name="message">Provider error message</param>
    /// <param name="innerException">Inner exception</param>
    /// <param name="providerName">Name of the provider</param>
    public EmailProviderException(string message, Exception innerException, string? providerName = null) : base(message, "ProviderError", innerException)
    {
        ProviderName = providerName;
    }
}
