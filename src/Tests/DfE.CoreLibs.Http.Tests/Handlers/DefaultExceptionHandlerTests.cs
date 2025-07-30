using DfE.CoreLibs.Http.Handlers;
using DfE.CoreLibs.Http.Interfaces;
using FluentAssertions;
using Xunit;

namespace DfE.CoreLibs.Http.Tests.Handlers
{
    public class DefaultExceptionHandlerTests
    {
        private readonly DefaultExceptionHandler _handler;

        public DefaultExceptionHandlerTests()
        {
            _handler = new DefaultExceptionHandler();
        }

        [Fact]
        public void Priority_ShouldBe100()
        {
            // Act & Assert
            _handler.Priority.Should().Be(100);
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException), true)]
        [InlineData(typeof(ArgumentException), true)]
        [InlineData(typeof(InvalidOperationException), true)]
        [InlineData(typeof(UnauthorizedAccessException), true)]
        [InlineData(typeof(NotImplementedException), true)]
        [InlineData(typeof(FileNotFoundException), true)]
        [InlineData(typeof(DirectoryNotFoundException), true)]
        [InlineData(typeof(TimeoutException), true)]
        [InlineData(typeof(Exception), false)]
        [InlineData(typeof(InvalidCastException), false)]
        public void CanHandle_ShouldReturnExpectedResult(Type exceptionType, bool expectedResult)
        {
            // Act
            var result = _handler.CanHandle(exceptionType);

            // Assert
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException), 400, "Invalid request: Required parameter is missing")]
        [InlineData(typeof(ArgumentException), 400, "Invalid request: Test argument exception")]
        [InlineData(typeof(InvalidOperationException), 400, "Invalid operation: Test invalid operation")]
        [InlineData(typeof(UnauthorizedAccessException), 401, "Unauthorized access")]
        [InlineData(typeof(NotImplementedException), 501, "Feature not implemented")]
        [InlineData(typeof(FileNotFoundException), 404, "Resource not found")]
        [InlineData(typeof(DirectoryNotFoundException), 404, "Directory not found")]
        [InlineData(typeof(TimeoutException), 408, "Request timeout")]
        public void Handle_ShouldReturnExpectedStatusCodeAndMessage(Type exceptionType, int expectedStatusCode, string expectedMessage)
        {
            // Arrange
            var exception = CreateException(exceptionType, "Test message");

            // Act
            var (statusCode, message) = _handler.Handle(exception);

            // Assert
            statusCode.Should().Be(expectedStatusCode);
            message.Should().Be(expectedMessage);
        }

        [Fact]
        public void Handle_ShouldReturnDefaultMessage_ForUnhandledExceptionType()
        {
            // Arrange
            var exception = new InvalidCastException("Test cast exception");

            // Act
            var (statusCode, message) = _handler.Handle(exception);

            // Assert
            statusCode.Should().Be(500);
            message.Should().Be("An unexpected error occurred");
        }

        [Fact]
        public void Handle_ShouldIncludeExceptionMessage_ForArgumentException()
        {
            // Arrange
            var exception = new ArgumentException("Custom argument error", "paramName");

            // Act
            var (statusCode, message) = _handler.Handle(exception);

            // Assert
            statusCode.Should().Be(400);
            message.Should().Be("Invalid request: Custom argument error");
        }

        [Fact]
        public void Handle_ShouldIncludeExceptionMessage_ForInvalidOperationException()
        {
            // Arrange
            var exception = new InvalidOperationException("Custom operation error");

            // Act
            var (statusCode, message) = _handler.Handle(exception);

            // Assert
            statusCode.Should().Be(400);
            message.Should().Be("Invalid operation: Custom operation error");
        }

        [Fact]
        public void Handle_ShouldAcceptContextParameter()
        {
            // Arrange
            var exception = new ArgumentException("Test exception");
            var context = new Dictionary<string, object> { ["test"] = "value" };

            // Act
            var (statusCode, message) = _handler.Handle(exception, context);

            // Assert
            statusCode.Should().Be(400);
            message.Should().Contain("Invalid request");
        }

        [Fact]
        public void Handle_ShouldAcceptNullContextParameter()
        {
            // Arrange
            var exception = new ArgumentException("Test exception");

            // Act
            var (statusCode, message) = _handler.Handle(exception, null);

            // Assert
            statusCode.Should().Be(400);
            message.Should().Contain("Invalid request");
        }

        [Fact]
        public void Handle_ShouldHandleNullExceptionMessage()
        {
            // Arrange
            var exception = new ArgumentException(null, "paramName");

            // Act
            var (statusCode, message) = _handler.Handle(exception);

            // Assert
            statusCode.Should().Be(400);
            message.Should().Be("Invalid request: ");
        }

        [Fact]
        public void Handle_ShouldHandleEmptyExceptionMessage()
        {
            // Arrange
            var exception = new ArgumentException("", "paramName");

            // Act
            var (statusCode, message) = _handler.Handle(exception);

            // Assert
            statusCode.Should().Be(400);
            message.Should().Be("Invalid request: ");
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException))]
        [InlineData(typeof(ArgumentException))]
        [InlineData(typeof(InvalidOperationException))]
        [InlineData(typeof(UnauthorizedAccessException))]
        [InlineData(typeof(NotImplementedException))]
        [InlineData(typeof(FileNotFoundException))]
        [InlineData(typeof(DirectoryNotFoundException))]
        [InlineData(typeof(TimeoutException))]
        public void Handle_ShouldWorkWithAllSupportedExceptionTypes(Type exceptionType)
        {
            // Arrange
            var exception = CreateException(exceptionType, "Test message");

            // Act
            var (statusCode, message) = _handler.Handle(exception);

            // Assert
            statusCode.Should().BeGreaterThan(0);
            message.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Handle_ShouldReturnConsistentResults_ForSameExceptionType()
        {
            // Arrange
            var exception1 = new ArgumentException("First message");
            var exception2 = new ArgumentException("Second message");

            // Act
            var (statusCode1, message1) = _handler.Handle(exception1);
            var (statusCode2, message2) = _handler.Handle(exception2);

            // Assert
            statusCode1.Should().Be(statusCode2);
            message1.Should().StartWith("Invalid request: ");
            message2.Should().StartWith("Invalid request: ");
        }

        [Fact]
        public void Handle_ShouldHandleInnerExceptions()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner error");
            var exception = new ArgumentException("Outer error", innerException);

            // Act
            var (statusCode, message) = _handler.Handle(exception);

            // Assert
            statusCode.Should().Be(400);
            message.Should().Be("Invalid request: Outer error");
        }

        private static Exception CreateException(Type exceptionType, string message)
        {
            return exceptionType switch
            {
                var t when t == typeof(ArgumentNullException) => new ArgumentNullException("paramName", message),
                var t when t == typeof(ArgumentException) => new ArgumentException(message),
                var t when t == typeof(InvalidOperationException) => new InvalidOperationException(message),
                var t when t == typeof(UnauthorizedAccessException) => new UnauthorizedAccessException(message),
                var t when t == typeof(NotImplementedException) => new NotImplementedException(message),
                var t when t == typeof(FileNotFoundException) => new FileNotFoundException(message),
                var t when t == typeof(DirectoryNotFoundException) => new DirectoryNotFoundException(message),
                var t when t == typeof(TimeoutException) => new TimeoutException(message),
                _ => throw new ArgumentException($"Unsupported exception type: {exceptionType.Name}")
            };
        }
    }
} 