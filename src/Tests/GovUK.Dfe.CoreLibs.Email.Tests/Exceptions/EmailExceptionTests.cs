using GovUK.Dfe.CoreLibs.Email.Exceptions;

namespace GovUK.Dfe.CoreLibs.Email.Tests.Exceptions;

public class EmailExceptionTests
{
    [Fact]
    public void EmailException_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Test error message";

        // Act
        var exception = new EmailException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void EmailException_WithMessageAndErrorCode_ShouldSetBoth()
    {
        // Arrange
        const string message = "Test error message";
        const string errorCode = "TEST_ERROR";

        // Act
        var exception = new EmailException(message, errorCode);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be(errorCode);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void EmailException_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        const string message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new EmailException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void EmailException_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        const string message = "Test error message";
        const string errorCode = "TEST_ERROR";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new EmailException(message, errorCode, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be(errorCode);
        exception.InnerException.Should().BeSameAs(innerException);
    }
}

public class EmailValidationExceptionTests
{
    [Fact]
    public void EmailValidationException_WithMessage_ShouldSetMessageAndErrorCode()
    {
        // Arrange
        const string message = "Validation failed";

        // Act
        var exception = new EmailValidationException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be("ValidationError");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void EmailValidationException_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        const string message = "Validation failed";
        var innerException = new ArgumentException("Invalid argument");

        // Act
        var exception = new EmailValidationException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be("ValidationError");
        exception.InnerException.Should().BeSameAs(innerException);
    }
}

public class EmailConfigurationExceptionTests
{
    [Fact]
    public void EmailConfigurationException_WithMessage_ShouldSetMessageAndErrorCode()
    {
        // Arrange
        const string message = "Configuration is invalid";

        // Act
        var exception = new EmailConfigurationException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be("ConfigurationError");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void EmailConfigurationException_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        const string message = "Configuration is invalid";
        var innerException = new FileNotFoundException("Config file not found");

        // Act
        var exception = new EmailConfigurationException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be("ConfigurationError");
        exception.InnerException.Should().BeSameAs(innerException);
    }
}

public class EmailProviderExceptionTests
{
    [Fact]
    public void EmailProviderException_WithMessage_ShouldSetMessageAndErrorCode()
    {
        // Arrange
        const string message = "Provider error occurred";

        // Act
        var exception = new EmailProviderException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be("ProviderError");
        exception.StatusCode.Should().BeNull();
        exception.ProviderName.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void EmailProviderException_WithMessageAndProvider_ShouldSetBoth()
    {
        // Arrange
        const string message = "Provider error occurred";
        const string providerName = "TestProvider";

        // Act
        var exception = new EmailProviderException(message, providerName);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be("ProviderError");
        exception.ProviderName.Should().Be(providerName);
        exception.StatusCode.Should().BeNull();
    }

    [Fact]
    public void EmailProviderException_WithMessageStatusCodeAndProvider_ShouldSetAll()
    {
        // Arrange
        const string message = "HTTP error occurred";
        const int statusCode = 429;
        const string providerName = "TestProvider";

        // Act
        var exception = new EmailProviderException(message, statusCode, providerName);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be("ProviderError");
        exception.StatusCode.Should().Be(statusCode);
        exception.ProviderName.Should().Be(providerName);
    }

    [Fact]
    public void EmailProviderException_WithMessageInnerExceptionAndProvider_ShouldSetAll()
    {
        // Arrange
        const string message = "Provider error occurred";
        var innerException = new HttpRequestException("Network error");
        const string providerName = "TestProvider";

        // Act
        var exception = new EmailProviderException(message, innerException, providerName);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be("ProviderError");
        exception.InnerException.Should().BeSameAs(innerException);
        exception.ProviderName.Should().Be(providerName);
        exception.StatusCode.Should().BeNull();
    }
}
