namespace GovUK.Dfe.CoreLibs.Email.Exceptions;

/// <summary>
/// Base exception for email-related errors
/// </summary>
public class EmailException : Exception
{
    /// <summary>
    /// Error code associated with the exception
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Creates a new EmailException
    /// </summary>
    /// <param name="message">Exception message</param>
    public EmailException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new EmailException with error code
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="errorCode">Error code</param>
    public EmailException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Creates a new EmailException with inner exception
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="innerException">Inner exception</param>
    public EmailException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a new EmailException with error code and inner exception
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="errorCode">Error code</param>
    /// <param name="innerException">Inner exception</param>
    public EmailException(string message, string errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
