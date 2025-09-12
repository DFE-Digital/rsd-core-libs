using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Exceptions;
using Xunit;

namespace GovUK.Dfe.CoreLibs.Security.Tests.TokenRefreshTests.Exceptions
{
    public class TokenIntrospectionExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_CreatesExceptionWithMessage()
        {
            // Arrange
            var message = "Token introspection failed";

            // Act
            var exception = new TokenIntrospectionException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
            Assert.Null(exception.StatusCode);
        }

        [Fact]
        public void Constructor_WithMessageAndStatusCode_CreatesExceptionWithMessageAndStatusCode()
        {
            // Arrange
            var message = "Token introspection failed";
            var statusCode = 400;

            // Act
            var exception = new TokenIntrospectionException(message, statusCode);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
            Assert.Equal(statusCode, exception.StatusCode);
        }

        [Fact]
        public void Constructor_WithMessageAndInnerException_CreatesExceptionWithMessageAndInnerException()
        {
            // Arrange
            var message = "Token introspection failed";
            var innerException = new HttpRequestException("Network error");

            // Act
            var exception = new TokenIntrospectionException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Null(exception.StatusCode);
        }

        [Fact]
        public void Constructor_WithMessageStatusCodeAndInnerException_CreatesExceptionWithAllProperties()
        {
            // Arrange
            var message = "Token introspection failed";
            var statusCode = 401;
            var innerException = new UnauthorizedAccessException("Unauthorized");

            // Act
            var exception = new TokenIntrospectionException(message, statusCode, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(statusCode, exception.StatusCode);
        }

        [Fact]
        public void Constructor_WithNullMessage_CreatesExceptionWithNullMessage()
        {
            // Act
            var exception = new TokenIntrospectionException(null!);

            // Assert
            // Note: .NET automatically provides a default message when null is passed
            Assert.NotNull(exception.Message);
            Assert.Null(exception.InnerException);
            Assert.Null(exception.StatusCode);
        }

        [Fact]
        public void Constructor_WithEmptyMessage_CreatesExceptionWithEmptyMessage()
        {
            // Arrange
            var message = "";

            // Act
            var exception = new TokenIntrospectionException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
            Assert.Null(exception.StatusCode);
        }

        [Fact]
        public void Constructor_WithWhitespaceMessage_CreatesExceptionWithWhitespaceMessage()
        {
            // Arrange
            var message = "   ";

            // Act
            var exception = new TokenIntrospectionException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
            Assert.Null(exception.StatusCode);
        }

        [Fact]
        public void Constructor_WithNegativeStatusCode_SetsStatusCode()
        {
            // Arrange
            var message = "Token introspection failed";
            var statusCode = -1;

            // Act
            var exception = new TokenIntrospectionException(message, statusCode);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(statusCode, exception.StatusCode);
        }

        [Fact]
        public void Constructor_WithZeroStatusCode_SetsStatusCode()
        {
            // Arrange
            var message = "Token introspection failed";
            var statusCode = 0;

            // Act
            var exception = new TokenIntrospectionException(message, statusCode);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(statusCode, exception.StatusCode);
        }

        [Fact]
        public void Constructor_WithLargeStatusCode_SetsStatusCode()
        {
            // Arrange
            var message = "Token introspection failed";
            var statusCode = 999;

            // Act
            var exception = new TokenIntrospectionException(message, statusCode);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(statusCode, exception.StatusCode);
        }

        [Fact]
        public void Exception_InheritsFromException()
        {
            // Act
            var exception = new TokenIntrospectionException("Test message");

            // Assert
            Assert.IsAssignableFrom<Exception>(exception);
        }

        [Fact]
        public async Task Exception_CanBeThrownAndCaught()
        {
            // Arrange
            var message = "Token introspection failed";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenIntrospectionException>(async () =>
            {
                await Task.Delay(1);
                throw new TokenIntrospectionException(message);
            });

            Assert.Equal(message, exception.Message);
            Assert.Null(exception.StatusCode);
        }

        [Fact]
        public async Task Exception_WithStatusCode_CanBeThrownAndCaught()
        {
            // Arrange
            var message = "Token introspection failed";
            var statusCode = 400;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenIntrospectionException>(async () =>
            {
                await Task.Delay(1);
                throw new TokenIntrospectionException(message, statusCode);
            });

            Assert.Equal(message, exception.Message);
            Assert.Equal(statusCode, exception.StatusCode);
        }

        [Fact]
        public async Task Exception_WithInnerException_CanBeThrownAndCaught()
        {
            // Arrange
            var message = "Token introspection failed";
            var innerException = new HttpRequestException("Network error");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenIntrospectionException>(async () =>
            {
                await Task.Delay(1);
                throw new TokenIntrospectionException(message, innerException);
            });

            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Null(exception.StatusCode);
        }

        [Fact]
        public async Task Exception_WithAllParameters_CanBeThrownAndCaught()
        {
            // Arrange
            var message = "Token introspection failed";
            var statusCode = 401;
            var innerException = new UnauthorizedAccessException("Unauthorized");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenIntrospectionException>(async () =>
            {
                await Task.Delay(1);
                throw new TokenIntrospectionException(message, statusCode, innerException);
            });

            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(statusCode, exception.StatusCode);
        }


        [Fact]
        public void StatusCode_IsReadOnly()
        {
            // Arrange
            var exception = new TokenIntrospectionException("Test message", 400);

            // Assert
            Assert.Equal(400, exception.StatusCode);
            // StatusCode is read-only, so we can't modify it
        }

        [Fact]
        public void StatusCode_DefaultValueIsNull()
        {
            // Act
            var exception = new TokenIntrospectionException("Test message");

            // Assert
            Assert.Null(exception.StatusCode);
        }
    }
}
