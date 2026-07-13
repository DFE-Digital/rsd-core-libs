namespace GovUK.Dfe.CoreLibs.SharePoint.Exceptions;

/// <summary>
/// Base exception for SharePoint operations.
/// </summary>
public class SharePointException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SharePointException"/> class.
    /// </summary>
    public SharePointException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SharePointException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SharePointException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SharePointException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SharePointException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a SharePoint file or folder is not found.
/// </summary>
public class SharePointNotFoundException : SharePointException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SharePointNotFoundException"/> class.
    /// </summary>
    public SharePointNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SharePointNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SharePointNotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SharePointNotFoundException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SharePointNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when SharePoint configuration is invalid.
/// </summary>
public class SharePointConfigurationException : SharePointException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SharePointConfigurationException"/> class.
    /// </summary>
    public SharePointConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SharePointConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SharePointConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SharePointConfigurationException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SharePointConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
