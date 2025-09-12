using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Exceptions;
using Xunit;

namespace GovUK.Dfe.CoreLibs.Security.Tests.TokenRefreshTests.Exceptions
{
    public class TokenRefreshExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_CreatesExceptionWithMessage()
        {
            // Arrange
            var message = "Token refresh failed";

            // Act
            var exception = new TokenRefreshException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor_WithMessageAndInnerException_CreatesExceptionWithMessageAndInnerException()
        {
            // Arrange
            var message = "Token refresh failed";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new TokenRefreshException(message, innerException: (Exception)innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Fact]
        public void Constructor_WithNullMessage_CreatesExceptionWithNullMessage()
        {
            // Act
            var exception = new TokenRefreshException(null!);

            // Assert
            // Note: .NET automatically provides a default message when null is passed
            Assert.NotNull(exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor_WithEmptyMessage_CreatesExceptionWithEmptyMessage()
        {
            // Arrange
            var message = "";

            // Act
            var exception = new TokenRefreshException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor_WithWhitespaceMessage_CreatesExceptionWithWhitespaceMessage()
        {
            // Arrange
            var message = "   ";

            // Act
            var exception = new TokenRefreshException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor_WithMessageAndNullInnerException_CreatesExceptionWithMessageAndNullInnerException()
        {
            // Arrange
            var message = "Token refresh failed";

            // Act
            var exception = new TokenRefreshException(message, innerException: null!);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Exception_InheritsFromException()
        {
            // Act
            var exception = new TokenRefreshException("Test message");

            // Assert
            Assert.IsAssignableFrom<Exception>(exception);
        }

        [Fact]
        public async Task Exception_CanBeThrownAndCaught()
        {
            // Arrange
            var message = "Token refresh failed";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenRefreshException>(async () =>
            {
                await Task.Delay(1);
                throw new TokenRefreshException(message);
            });

            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public async Task Exception_WithInnerException_CanBeThrownAndCaught()
        {
            // Arrange
            var message = "Token refresh failed";
            var innerException = new HttpRequestException("Network error");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenRefreshException>(async () =>
            {
                await Task.Delay(1);
                throw new TokenRefreshException(message, innerException);
            });

            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Fact]
        public async Task Exception_WithErrorCode_CanBeThrownAndCaught()
        {
            // Arrange
            var message = "Token refresh failed";
            var errorCode = "invalid_grant";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenRefreshException>(async () =>
            {
                await Task.Delay(1);
                throw new TokenRefreshException(message, errorCode);
            });

            Assert.Equal(message, exception.Message);
            Assert.Equal(errorCode, exception.ErrorCode);
        }


        [Fact]
        public void Constructor_WithMessageAndErrorCode_SetsErrorCode()
        {
            // Arrange
            var message = "Token refresh failed";
            var errorCode = "invalid_grant";

            // Act
            var exception = new TokenRefreshException(message, errorCode);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(errorCode, exception.ErrorCode);
            Assert.Null(exception.StatusCode);
        }

        [Fact]
        public void Constructor_WithMessageErrorCodeAndStatusCode_SetsAllProperties()
        {
            // Arrange
            var message = "Token refresh failed";
            var errorCode = "invalid_grant";
            var statusCode = 400;

            // Act
            var exception = new TokenRefreshException(message, errorCode, statusCode);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(errorCode, exception.ErrorCode);
            Assert.Equal(statusCode, exception.StatusCode);
        }

        [Fact]
        public void Constructor_WithMessageErrorCodeAndInnerException_SetsErrorCodeAndInnerException()
        {
            // Arrange
            var message = "Token refresh failed";
            var errorCode = "invalid_grant";
            var innerException = new HttpRequestException("Network error");

            // Act
            var exception = new TokenRefreshException(message, errorCode, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(errorCode, exception.ErrorCode);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Null(exception.StatusCode);
        }
    }
}
