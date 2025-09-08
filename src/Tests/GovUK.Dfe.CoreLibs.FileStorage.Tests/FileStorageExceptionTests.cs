using GovUK.Dfe.CoreLibs.FileStorage.Exceptions;
using Xunit;
using FileNotFoundException = GovUK.Dfe.CoreLibs.FileStorage.Exceptions.FileNotFoundException;

namespace GovUK.Dfe.CoreLibs.FileStorage.Tests;

public class FileStorageExceptionTests
{
    [Fact]
    public void FileStorageException_DefaultConstructor_ShouldCreateException()
    {
        // Act
        var exception = new FileStorageException();

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<FileStorageException>(exception);
    }

    [Fact]
    public void FileStorageException_WithMessage_ShouldCreateExceptionWithMessage()
    {
        // Arrange
        var message = "Test exception message";

        // Act
        var exception = new FileStorageException(message);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void FileStorageException_WithMessageAndInnerException_ShouldCreateExceptionWithMessageAndInnerException()
    {
        // Arrange
        var message = "Test exception message";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new FileStorageException(message, innerException);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void FileNotFoundException_DefaultConstructor_ShouldCreateException()
    {
        // Act
        var exception = new FileNotFoundException();

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<FileNotFoundException>(exception);
    }

    [Fact]
    public void FileNotFoundException_WithMessage_ShouldCreateExceptionWithMessage()
    {
        // Arrange
        var message = "File not found message";

        // Act
        var exception = new FileNotFoundException(message);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void FileNotFoundException_WithMessageAndInnerException_ShouldCreateExceptionWithMessageAndInnerException()
    {
        // Arrange
        var message = "File not found message";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new FileNotFoundException(message, innerException);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void FileStorageConfigurationException_DefaultConstructor_ShouldCreateException()
    {
        // Act
        var exception = new FileStorageConfigurationException();

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<FileStorageConfigurationException>(exception);
    }

    [Fact]
    public void FileStorageConfigurationException_WithMessage_ShouldCreateExceptionWithMessage()
    {
        // Arrange
        var message = "Configuration error message";

        // Act
        var exception = new FileStorageConfigurationException(message);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void FileStorageConfigurationException_WithMessageAndInnerException_ShouldCreateExceptionWithMessageAndInnerException()
    {
        // Arrange
        var message = "Configuration error message";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new FileStorageConfigurationException(message, innerException);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void ExceptionHierarchy_ShouldBeCorrect()
    {
        // Act
        var fileStorageException = new FileStorageException("Base exception");
        var fileNotFoundException = new FileNotFoundException("File not found");
        var configurationException = new FileStorageConfigurationException("Configuration error");

        // Assert
        Assert.IsAssignableFrom<Exception>(fileStorageException);
        Assert.IsAssignableFrom<FileStorageException>(fileNotFoundException);
        Assert.IsAssignableFrom<FileStorageException>(configurationException);
        Assert.IsAssignableFrom<Exception>(fileNotFoundException);
        Assert.IsAssignableFrom<Exception>(configurationException);
    }
}
