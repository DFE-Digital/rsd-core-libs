namespace DfE.CoreLibs.FileStorage.Exceptions;

/// <summary>
/// Base exception for file storage operations.
/// </summary>
public class FileStorageException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileStorageException"/> class.
    /// </summary>
    public FileStorageException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStorageException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public FileStorageException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStorageException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public FileStorageException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a file is not found.
/// </summary>
public class FileNotFoundException : FileStorageException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileNotFoundException"/> class.
    /// </summary>
    public FileNotFoundException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public FileNotFoundException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileNotFoundException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public FileNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when there are configuration issues.
/// </summary>
public class FileStorageConfigurationException : FileStorageException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileStorageConfigurationException"/> class.
    /// </summary>
    public FileStorageConfigurationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStorageConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public FileStorageConfigurationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStorageConfigurationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public FileStorageConfigurationException(string message, Exception innerException) : base(message, innerException) { }
} 