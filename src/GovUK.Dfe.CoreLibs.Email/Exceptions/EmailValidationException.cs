namespace GovUK.Dfe.CoreLibs.Email.Exceptions;

/// <summary>
/// Exception thrown when email validation fails
/// </summary>
public class EmailValidationException : EmailException
{
    /// <summary>
    /// Creates a new EmailValidationException
    /// </summary>
    /// <param name="message">Validation error message</param>
    public EmailValidationException(string message) : base(message, "ValidationError")
    {
    }

    /// <summary>
    /// Creates a new EmailValidationException with inner exception
    /// </summary>
    /// <param name="message">Validation error message</param>
    /// <param name="innerException">Inner exception</param>
    public EmailValidationException(string message, Exception innerException) : base(message, "ValidationError", innerException)
    {
    }
}
