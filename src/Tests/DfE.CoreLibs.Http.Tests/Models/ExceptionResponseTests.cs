using DfE.CoreLibs.Http.Models;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace DfE.CoreLibs.Http.Tests.Models
{
    public class ExceptionResponseTests
    {
        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Act
            var response = new ExceptionResponse();

            // Assert
            response.ErrorId.Should().BeEmpty();
            response.StatusCode.Should().Be(0);
            response.Message.Should().BeEmpty();
            response.Details.Should().BeNull();
            response.ExceptionType.Should().BeEmpty();
            response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            response.CorrelationId.Should().BeNull();
            response.Context.Should().BeNull();
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var response = new ExceptionResponse();
            var errorId = "123456";
            var statusCode = 400;
            var message = "Test error message";
            var details = "Test details";
            var exceptionType = "ArgumentException";
            var timestamp = DateTime.UtcNow;
            var correlationId = "test-correlation-id";
            var context = new Dictionary<string, object> { ["test"] = "value" };

            // Act
            response.ErrorId = errorId;
            response.StatusCode = statusCode;
            response.Message = message;
            response.Details = details;
            response.ExceptionType = exceptionType;
            response.Timestamp = timestamp;
            response.CorrelationId = correlationId;
            response.Context = context;

            // Assert
            response.ErrorId.Should().Be(errorId);
            response.StatusCode.Should().Be(statusCode);
            response.Message.Should().Be(message);
            response.Details.Should().Be(details);
            response.ExceptionType.Should().Be(exceptionType);
            response.Timestamp.Should().Be(timestamp);
            response.CorrelationId.Should().Be(correlationId);
            response.Context.Should().BeEquivalentTo(context);
        }

        [Fact]
        public void JsonSerialization_ShouldUseCamelCase()
        {
            // Arrange
            var response = new ExceptionResponse
            {
                ErrorId = "123456",
                StatusCode = 400,
                Message = "Test error message",
                Details = "Test details",
                ExceptionType = "ArgumentException",
                Timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                CorrelationId = "test-correlation-id",
                Context = new Dictionary<string, object> { ["test"] = "value" }
            };

            // Act
            var json = JsonSerializer.Serialize(response);

            // Assert
            json.Should().Contain("\"errorId\":\"123456\"");
            json.Should().Contain("\"statusCode\":400");
            json.Should().Contain("\"message\":\"Test error message\"");
            json.Should().Contain("\"details\":\"Test details\"");
            json.Should().Contain("\"exceptionType\":\"ArgumentException\"");
            json.Should().Contain("\"timestamp\":\"2024-01-15T10:30:00Z\"");
            json.Should().Contain("\"correlationId\":\"test-correlation-id\"");
            json.Should().Contain("\"context\":{\"test\":\"value\"}");
        }

        [Fact]
        public void JsonDeserialization_ShouldWorkCorrectly()
        {
            // Arrange
            var json = @"{
                ""errorId"": ""123456"",
                ""statusCode"": 400,
                ""message"": ""Test error message"",
                ""details"": ""Test details"",
                ""exceptionType"": ""ArgumentException"",
                ""timestamp"": ""2024-01-15T10:30:00.000Z"",
                ""correlationId"": ""test-correlation-id"",
                ""context"": { ""test"": ""value"" }
            }";

            // Act
            var response = JsonSerializer.Deserialize<ExceptionResponse>(json);

            // Assert
            response.Should().NotBeNull();
            response!.ErrorId.Should().Be("123456");
            response.StatusCode.Should().Be(400);
            response.Message.Should().Be("Test error message");
            response.Details.Should().Be("Test details");
            response.ExceptionType.Should().Be("ArgumentException");
            response.Timestamp.Should().Be(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
            response.CorrelationId.Should().Be("test-correlation-id");
            response.Context.Should().ContainKey("test");
            response.Context!["test"].ToString().Should().Be("value");
        }

        [Fact]
        public void JsonDeserialization_WithNullValues_ShouldWorkCorrectly()
        {
            // Arrange
            var json = @"{
                ""errorId"": ""123456"",
                ""statusCode"": 400,
                ""message"": ""Test error message"",
                ""details"": null,
                ""exceptionType"": ""ArgumentException"",
                ""timestamp"": ""2024-01-15T10:30:00.000Z"",
                ""correlationId"": null,
                ""context"": null
            }";

            // Act
            var response = JsonSerializer.Deserialize<ExceptionResponse>(json);

            // Assert
            response.Should().NotBeNull();
            response!.ErrorId.Should().Be("123456");
            response.StatusCode.Should().Be(400);
            response.Message.Should().Be("Test error message");
            response.Details.Should().BeNull();
            response.ExceptionType.Should().Be("ArgumentException");
            response.Timestamp.Should().Be(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
            response.CorrelationId.Should().BeNull();
            response.Context.Should().BeNull();
        }

        [Fact]
        public void JsonSerialization_WithNullValues_ShouldExcludeNulls()
        {
            // Arrange
            var response = new ExceptionResponse
            {
                ErrorId = "123456",
                StatusCode = 400,
                Message = "Test error message",
                Details = null,
                ExceptionType = "ArgumentException",
                Timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                CorrelationId = null,
                Context = null
            };

            // Act
            var json = JsonSerializer.Serialize(response);

            // Assert
            json.Should().Contain("\"errorId\":\"123456\"");
            json.Should().Contain("\"statusCode\":400");
            json.Should().Contain("\"message\":\"Test error message\"");
            json.Should().Contain("\"exceptionType\":\"ArgumentException\"");
            json.Should().Contain("\"timestamp\":\"2024-01-15T10:30:00Z\"");
            json.Should().Contain("\"details\":null");
            json.Should().Contain("\"correlationId\":null");
            json.Should().Contain("\"context\":null");
        }

        [Fact]
        public void Context_ShouldSupportComplexObjects()
        {
            // Arrange
            var response = new ExceptionResponse
            {
                Context = new Dictionary<string, object>
                {
                    ["string"] = "value",
                    ["number"] = 42,
                    ["boolean"] = true,
                    ["array"] = new[] { 1, 2, 3 },
                    ["object"] = new Dictionary<string, object> { ["nested"] = "value" }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(response);

            // Assert
            json.Should().Contain("\"string\":\"value\"");
            json.Should().Contain("\"number\":42");
            json.Should().Contain("\"boolean\":true");
            json.Should().Contain("\"array\":[1,2,3]");
            json.Should().Contain("\"object\":{\"nested\":\"value\"}");
        }

        [Fact]
        public void Context_ShouldDeserializeComplexObjects()
        {
            // Arrange
            var json = @"{
                ""errorId"": ""123456"",
                ""statusCode"": 400,
                ""message"": ""Test error message"",
                ""context"": {
                    ""string"": ""value"",
                    ""number"": 42,
                    ""boolean"": true,
                    ""array"": [1, 2, 3],
                    ""object"": { ""nested"": ""value"" }
                }
            }";

            // Act
            var response = JsonSerializer.Deserialize<ExceptionResponse>(json);

            // Assert
            response.Should().NotBeNull();
            response!.Context.Should().NotBeNull();
            response.Context!["string"].ToString().Should().Be("value");
            ((JsonElement)response.Context["number"]).GetInt32().Should().Be(42);
            ((JsonElement)response.Context["boolean"]).GetBoolean().Should().Be(true);
            response.Context["array"].Should().BeOfType<JsonElement>();
            response.Context["object"].Should().BeOfType<JsonElement>();
        }

        [Fact]
        public void Timestamp_ShouldDefaultToUtcNow()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var response = new ExceptionResponse();

            // Assert
            response.Timestamp.Should().BeAfter(beforeCreation);
            response.Timestamp.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public void Equals_ShouldWorkCorrectly()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var response1 = new ExceptionResponse
            {
                ErrorId = "123456",
                StatusCode = 400,
                Message = "Test error message",
                Timestamp = timestamp
            };

            var response2 = new ExceptionResponse
            {
                ErrorId = "123456",
                StatusCode = 400,
                Message = "Test error message",
                Timestamp = timestamp
            };

            var response3 = new ExceptionResponse
            {
                ErrorId = "654321",
                StatusCode = 500,
                Message = "Different error message",
                Timestamp = timestamp
            };

            // Act & Assert
            response1.Should().BeEquivalentTo(response2);
            response1.Should().NotBeEquivalentTo(response3);
        }

        [Fact]
        public void ToString_ShouldReturnTypeName()
        {
            // Arrange
            var response = new ExceptionResponse
            {
                ErrorId = "123456",
                StatusCode = 400,
                Message = "Test error message",
                ExceptionType = "ArgumentException"
            };

            // Act
            var result = response.ToString();

            // Assert
            result.Should().Be("DfE.CoreLibs.Http.Models.ExceptionResponse");
        }
    }
} 