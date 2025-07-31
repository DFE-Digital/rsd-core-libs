using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Http.Configuration;
using DfE.CoreLibs.Http.Handlers;
using DfE.CoreLibs.Http.Interfaces;
using DfE.CoreLibs.Http.Middlewares.ExceptionHandler;
using DfE.CoreLibs.Http.Models;
using DfE.CoreLibs.Http.Utils;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.AutoFixture.Customizations;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NSubstitute.ExceptionExtensions;

namespace DfE.CoreLibs.Http.Tests.Middlewares
{
    public class GlobalExceptionHandlerMiddlewareTests
    {
        private readonly RequestDelegate _nextDelegate;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly ExceptionHandlerOptions _options;
        private readonly GlobalExceptionHandlerMiddleware _middleware;

        public GlobalExceptionHandlerMiddlewareTests()
        {
            _nextDelegate = Substitute.For<RequestDelegate>();
            _logger = Substitute.For<ILogger<GlobalExceptionHandlerMiddleware>>();
            _options = new ExceptionHandlerOptions
            {
                IncludeDetails = false,
                LogExceptions = true,
                DefaultErrorMessage = "An unexpected error occurred",
                IncludeCorrelationId = true
            };
            _middleware = new GlobalExceptionHandlerMiddleware(_nextDelegate, _logger, _options);
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldCallNextDelegate_WhenNoExceptionOccurs(DefaultHttpContext context)
        {
            // Arrange
            context.Response.Body = new MemoryStream();
            _nextDelegate.Invoke(context).Returns(Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            await _nextDelegate.Received(1).Invoke(context);
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldHandleException_WhenExceptionOccurs(DefaultHttpContext context)
        {
            // Arrange
            var exception = new ArgumentException("Test exception");
            context.Response.Body = new MemoryStream();
            var services = new ServiceCollection();
            services.AddSingleton<IEnumerable<ICustomExceptionHandler>>(Enumerable.Empty<ICustomExceptionHandler>());
            context.RequestServices = services.BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(400); // DefaultExceptionHandler handles ArgumentException with 400
            context.Response.ContentType.Should().Be("application/json");
            
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            Regex.IsMatch(errorResponse!.ErrorId, @"^\d{6}$").Should().BeTrue(); // 6-digit random
            errorResponse.StatusCode.Should().Be(400);
            errorResponse.Message.Should().Be("Invalid request: Test exception");
            errorResponse.ExceptionType.Should().Be("ArgumentException");
            errorResponse.Details.Should().BeNull(); // IncludeDetails = false
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldIncludeDetails_WhenIncludeDetailsIsTrue(DefaultHttpContext context)
        {
            // Arrange
            _options.IncludeDetails = true;
            var exception = new InvalidOperationException("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.Details.Should().Contain("InvalidOperationException");
            errorResponse.Details.Should().Contain("Test exception");
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldNotLogException_WhenLogExceptionsIsFalse(DefaultHttpContext context)
        {
            // Arrange
            _options.LogExceptions = false;
            var exception = new Exception("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            _logger.DidNotReceive().Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>());
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldLogException_WhenLogExceptionsIsTrue(DefaultHttpContext context)
        {
            // Arrange
            _options.LogExceptions = true;
            var exception = new Exception("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                exception,
                Arg.Any<Func<object, Exception, string>>());
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldIncludeCorrelationId_WhenAvailable(DefaultHttpContext context)
        {
            // Arrange
            var correlationContext = Substitute.For<ICorrelationContext>();
            var correlationId = Guid.NewGuid();
            correlationContext.CorrelationId.Returns(correlationId);
            
            var services = new ServiceCollection();
            services.AddSingleton<IEnumerable<ICustomExceptionHandler>>(Enumerable.Empty<ICustomExceptionHandler>());
            services.AddSingleton(correlationContext);
            var serviceProvider = services.BuildServiceProvider();
            context.RequestServices = serviceProvider;

            var exception = new Exception("Test exception");
            context.Response.Body = new MemoryStream();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.CorrelationId.Should().Be(correlationId.ToString());
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldNotIncludeCorrelationId_WhenIncludeCorrelationIdIsFalse(DefaultHttpContext context)
        {
            // Arrange
            _options.IncludeCorrelationId = false;
            var exception = new Exception("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.CorrelationId.Should().BeNull();
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldUseCustomErrorIdGenerator_WhenProvided(DefaultHttpContext context)
        {
            // Arrange
            var customErrorId = "CUSTOM-123";
            _options.ErrorIdGenerator = () => customErrorId;
            
            var exception = new Exception("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.ErrorId.Should().Be(customErrorId);
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldUseDefaultErrorIdGenerator_WhenNotProvided(DefaultHttpContext context)
        {
            // Arrange
            _options.ErrorIdGenerator = null;
            var exception = new Exception("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            Regex.IsMatch(errorResponse!.ErrorId, @"^\d{6}$").Should().BeTrue();
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldReThrowIgnoredExceptions(DefaultHttpContext context)
        {
            // Arrange
            _options.IgnoredExceptionTypes.Add(typeof(ArgumentException));
            var exception = new ArgumentException("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<ArgumentException>(() => 
                _middleware.InvokeAsync(context));
            
            thrownException.Message.Should().Be("Test exception");
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldHandleCustomExceptionHandler(DefaultHttpContext context)
        {
            // Arrange
            var customHandler = Substitute.For<ICustomExceptionHandler>();
            customHandler.Priority.Returns(10);
            customHandler.CanHandle(typeof(ArgumentException)).Returns(true);
            customHandler.Handle(Arg.Any<Exception>(), Arg.Any<Dictionary<string, object>>())
                       .Returns(new ExceptionResponse
                       {
                           StatusCode = 422,
                           Message = "Custom validation error",
                           ExceptionType = "ArgumentException"
                       });

            _options.CustomHandlers.Add(customHandler);
            var newMiddleware = new GlobalExceptionHandlerMiddleware(_nextDelegate, _logger, _options);

            var exception = new ArgumentException("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await newMiddleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(422);
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.Message.Should().Be("Custom validation error");
            errorResponse.StatusCode.Should().Be(422);
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldUseDefaultHandler_WhenCustomHandlerDoesNotMatch(DefaultHttpContext context)
        {
            // Arrange
            var customHandler = Substitute.For<ICustomExceptionHandler>();
            customHandler.Priority.Returns(10);
            customHandler.CanHandle(typeof(ArgumentException)).Returns(false);

            _options.CustomHandlers.Add(customHandler);
            var newMiddleware = new GlobalExceptionHandlerMiddleware(_nextDelegate, _logger, _options);

            var exception = new ArgumentException("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await newMiddleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(400); // Default handler for ArgumentException
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.Message.Should().Contain("Invalid request");
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldExecuteSharedPostProcessingAction(DefaultHttpContext context)
        {
            // Arrange
            var postProcessingCalled = false;
            _options.SharedPostProcessingAction = (exception, response) =>
            {
                postProcessingCalled = true;
                response.Context = new Dictionary<string, object> { ["processed"] = true };
            };

            var exception = new Exception("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            postProcessingCalled.Should().BeTrue();
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.Context.Should().ContainKey("processed");
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldHandlePostProcessingException(DefaultHttpContext context)
        {
            // Arrange
            _options.SharedPostProcessingAction = (exception, response) =>
            {
                throw new InvalidOperationException("Post-processing error");
            };

            var exception = new Exception("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            // Should not throw, but log the post-processing error
            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString().Contains("Error during shared post-processing action")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>());
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldPassContextToHandlers(DefaultHttpContext context)
        {
            // Arrange
            Dictionary<string, object>? passedContext = null;
            var customHandler = Substitute.For<ICustomExceptionHandler>();
            customHandler.Priority.Returns(10);
            customHandler.CanHandle(typeof(ArgumentException)).Returns(true);
            customHandler.Handle(Arg.Any<Exception>(), Arg.Any<Dictionary<string, object>>())
                       .Returns(new ExceptionResponse
                       {
                           StatusCode = 422,
                           Message = "Custom error",
                           ExceptionType = "ArgumentException"
                       })
                       .AndDoes(callInfo => passedContext = callInfo.Arg<Dictionary<string, object>>());

            _options.CustomHandlers.Add(customHandler);
            var newMiddleware = new GlobalExceptionHandlerMiddleware(_nextDelegate, _logger, _options);

            var exception = new ArgumentException("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await newMiddleware.InvokeAsync(context);

            // Assert
            passedContext.Should().NotBeNull();
            passedContext.Should().BeEmpty(); // Initially empty
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldIncludeContextInResponse_WhenHandlerAddsContext(DefaultHttpContext context)
        {
            // Arrange
            var customHandler = Substitute.For<ICustomExceptionHandler>();
            customHandler.Priority.Returns(10);
            customHandler.CanHandle(typeof(ArgumentException)).Returns(true);
            customHandler.Handle(Arg.Any<Exception>(), Arg.Any<Dictionary<string, object>>())
                       .Returns(new ExceptionResponse
                       {
                           StatusCode = 422,
                           Message = "Custom error",
                           ExceptionType = "ArgumentException",
                           Context = new Dictionary<string, object> { ["testKey"] = "testValue" }
                       });

            _options.CustomHandlers.Add(customHandler);
            var newMiddleware = new GlobalExceptionHandlerMiddleware(_nextDelegate, _logger, _options);

            var exception = new ArgumentException("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await newMiddleware.InvokeAsync(context);

            // Assert
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.Context.Should().ContainKey("testKey");
            errorResponse.Context!["testKey"].ToString().Should().Be("testValue");
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldNotIncludeContextInResponse_WhenEmpty(DefaultHttpContext context)
        {
            // Arrange
            var exception = new Exception("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.Context.Should().BeNull();
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldHandleMultipleCustomHandlers_WithPriority(DefaultHttpContext context)
        {
            // Arrange
            var highPriorityHandler = Substitute.For<ICustomExceptionHandler>();
            highPriorityHandler.Priority.Returns(5);
            highPriorityHandler.CanHandle(typeof(ArgumentException)).Returns(true);
            highPriorityHandler.Handle(Arg.Any<Exception>(), Arg.Any<Dictionary<string, object>>())
                             .Returns(new ExceptionResponse
                             {
                                 StatusCode = 422,
                                 Message = "High priority error",
                                 ExceptionType = "ArgumentException"
                             });

            var lowPriorityHandler = Substitute.For<ICustomExceptionHandler>();
            lowPriorityHandler.Priority.Returns(15);
            lowPriorityHandler.CanHandle(typeof(ArgumentException)).Returns(true);
            lowPriorityHandler.Handle(Arg.Any<Exception>(), Arg.Any<Dictionary<string, object>>())
                            .Returns(new ExceptionResponse
                            {
                                StatusCode = 400,
                                Message = "Low priority error",
                                ExceptionType = "ArgumentException"
                            });

            _options.CustomHandlers.Add(lowPriorityHandler); // Add low priority first
            _options.CustomHandlers.Add(highPriorityHandler); // Add high priority second
            var newMiddleware = new GlobalExceptionHandlerMiddleware(_nextDelegate, _logger, _options);

            var exception = new ArgumentException("Test exception");
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection().BuildServiceProvider();
            _nextDelegate.Invoke(context).Throws(exception);

            // Act
            await newMiddleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(422);
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.Message.Should().Be("High priority error");
            
            // Verify high priority handler was called, low priority was not
            highPriorityHandler.Received(1).Handle(Arg.Any<Exception>(), Arg.Any<Dictionary<string, object>>());
            lowPriorityHandler.DidNotReceive().Handle(Arg.Any<Exception>(), Arg.Any<Dictionary<string, object>>());
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new GlobalExceptionHandlerMiddleware(_nextDelegate, _logger, null!));
            
            exception.ParamName.Should().Be("options");
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public void Constructor_ShouldThrowArgumentNullException_WhenNextDelegateIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new GlobalExceptionHandlerMiddleware(null!, _logger, _options));
            
            exception.ParamName.Should().Be("next");
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new GlobalExceptionHandlerMiddleware(_nextDelegate, null!, _options));
            
            exception.ParamName.Should().Be("logger");
        }

        private static string ReadResponseBody(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            return reader.ReadToEnd();
        }
    }
} 