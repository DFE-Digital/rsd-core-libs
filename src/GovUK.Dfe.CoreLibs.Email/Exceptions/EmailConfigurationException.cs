namespace GovUK.Dfe.CoreLibs.Email.Exceptions;

/// <summary>
/// Exception thrown when email service configuration is invalid
/// </summary>
public class EmailConfigurationException : EmailException
{
    /// <summary>
    /// Creates a new EmailConfigurationException
    /// </summary>
    /// <param name="message">Configuration error message</param>
    public EmailConfigurationException(string message) : base(message, "ConfigurationError")
    {
    }

    /// <summary>
    /// Creates a new EmailConfigurationException with inner exception
    /// </summary>
    /// <param name="message">Configuration error message</param>
    /// <param name="innerException">Inner exception</param>
    public EmailConfigurationException(string message, Exception innerException) : base(message, "ConfigurationError", innerException)
    {
    }
}
