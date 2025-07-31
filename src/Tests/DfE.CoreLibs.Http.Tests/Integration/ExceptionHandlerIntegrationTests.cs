using DfE.CoreLibs.Http.Configuration;
using DfE.CoreLibs.Http.Extensions;
using DfE.CoreLibs.Http.Interfaces;
using DfE.CoreLibs.Http.Middlewares.ExceptionHandler;
using DfE.CoreLibs.Http.Models;
using DfE.CoreLibs.Http.Utils;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Xunit;
using ExceptionHandlerOptions = DfE.CoreLibs.Http.Configuration.ExceptionHandlerOptions;

namespace DfE.CoreLibs.Http.Tests.Integration
{
    public class ExceptionHandlerIntegrationTests
    {
        [Fact]
        public async Task Middleware_ShouldHandleBasicException_WithDefaultConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var options = new ExceptionHandlerOptions();
            var logger = serviceProvider.GetRequiredService<ILogger<GlobalExceptionHandlerMiddleware>>();
            var middleware = new GlobalExceptionHandlerMiddleware(
                async (ctx) => { throw new Exception("Test exception"); },
                logger,
                options
            );

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(500);
            context.Response.ContentType.Should().Be("application/json");

            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            Regex.IsMatch(errorResponse!.ErrorId, @"^\d{6}$").Should().BeTrue();
            errorResponse.StatusCode.Should().Be(500);
            errorResponse.Message.Should().Be("An unexpected error occurred");
        }

        [Fact]
        public async Task Middleware_ShouldHandleCustomExceptionHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var options = new ExceptionHandlerOptions();
            options.CustomHandlers.Add(new TestCustomExceptionHandler());
            var logger = serviceProvider.GetRequiredService<ILogger<GlobalExceptionHandlerMiddleware>>();
            var middleware = new GlobalExceptionHandlerMiddleware(
                async (ctx) => { throw new ArgumentException("Test exception"); },
                logger,
                options
            );

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            // Act
            await middleware.InvokeAsync(context);

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

        [Fact]
        public async Task Middleware_ShouldUseCustomErrorIdGenerator()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var customErrorId = "CUSTOM-123";
            var options = new ExceptionHandlerOptions();
            options.WithCustomErrorIdGenerator(() => customErrorId);
            
            var logger = serviceProvider.GetRequiredService<ILogger<GlobalExceptionHandlerMiddleware>>();
            var middleware = new GlobalExceptionHandlerMiddleware(
                async (ctx) => { throw new Exception("Test exception"); },
                logger,
                options
            );

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            // Act
            await middleware.InvokeAsync(context);

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

        [Fact]
        public async Task Middleware_ShouldUseEnvironmentAwareErrorIds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var options = new ExceptionHandlerOptions();
            options.WithEnvironmentAwareErrorIds("Development");
            
            var logger = serviceProvider.GetRequiredService<ILogger<GlobalExceptionHandlerMiddleware>>();
            var middleware = new GlobalExceptionHandlerMiddleware(
                async (ctx) => { throw new Exception("Test exception"); },
                logger,
                options
            );

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            Regex.IsMatch(errorResponse!.ErrorId, @"^D-\d{6}$").Should().BeTrue();
        }

        [Fact]
        public async Task Middleware_ShouldExecuteSharedPostProcessing()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var postProcessingCalled = false;
            var options = new ExceptionHandlerOptions();
            options.WithSharedPostProcessing((exception, response) =>
            {
                postProcessingCalled = true;
                response.Context = new Dictionary<string, object> { ["processed"] = true };
            });

            // Create the middleware directly
            var logger = serviceProvider.GetRequiredService<ILogger<GlobalExceptionHandlerMiddleware>>();
            var middleware = new GlobalExceptionHandlerMiddleware(
                async (ctx) => { throw new Exception("Test exception"); },
                logger,
                options
            );

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            // Act
            await middleware.InvokeAsync(context);

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

        [Fact]
        public async Task Middleware_ShouldIncludeCorrelationId_WhenAvailable()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var options = new ExceptionHandlerOptions();
            options.IncludeCorrelationId = true;
            
            var logger = serviceProvider.GetRequiredService<ILogger<GlobalExceptionHandlerMiddleware>>();
            var middleware = new GlobalExceptionHandlerMiddleware(
                async (ctx) => { throw new Exception("Test exception"); },
                logger,
                options
            );

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            // Mock correlation context
            var correlationContext = Substitute.For<ICorrelationContext>();
            var correlationId = Guid.NewGuid();
            correlationContext.CorrelationId.Returns(correlationId);
            
            var mockServiceProvider = Substitute.For<IServiceProvider>();
            mockServiceProvider.GetService(typeof(ICorrelationContext)).Returns(correlationContext);
            context.RequestServices = mockServiceProvider;

            // Act
            await middleware.InvokeAsync(context);

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

        [Fact]
        public async Task Middleware_ShouldHandleMultipleCustomHandlers_WithPriority()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var options = new ExceptionHandlerOptions();
            options.CustomHandlers.Add(new LowPriorityTestHandler());
            options.CustomHandlers.Add(new HighPriorityTestHandler());
            var logger = serviceProvider.GetRequiredService<ILogger<GlobalExceptionHandlerMiddleware>>();
            var middleware = new GlobalExceptionHandlerMiddleware(
                async (ctx) => { throw new ArgumentException("Test exception"); },
                logger,
                options
            );

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(422); // High priority handler should be used
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.Message.Should().Be("High priority error");
        }

        [Fact]
        public async Task Middleware_ShouldIgnoreSpecifiedExceptionTypes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var options = new ExceptionHandlerOptions();
            options.IgnoredExceptionTypes.Add(typeof(ArgumentException));
            
            var logger = serviceProvider.GetRequiredService<ILogger<GlobalExceptionHandlerMiddleware>>();
            var middleware = new GlobalExceptionHandlerMiddleware(
                async (ctx) => { throw new ArgumentException("Test exception"); },
                logger,
                options
            );

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => middleware.InvokeAsync(context));
            exception.Message.Should().Be("Test exception");
        }

        [Fact]
        public async Task Middleware_ShouldHandleContextInCustomHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var options = new ExceptionHandlerOptions();
            options.CustomHandlers.Add(new ContextAwareTestHandler());
            var logger = serviceProvider.GetRequiredService<ILogger<GlobalExceptionHandlerMiddleware>>();
            var middleware = new GlobalExceptionHandlerMiddleware(
                async (ctx) => { throw new ArgumentException("Test exception"); },
                logger,
                options
            );

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.Context.Should().ContainKey("handlerContext");
            errorResponse.Context!["handlerContext"].ToString().Should().Be("testValue");
        }

        [Fact]
        public async Task Middleware_ShouldHandlePostProcessingExceptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var options = new ExceptionHandlerOptions();
            options.WithSharedPostProcessing((exception, response) =>
            {
                throw new InvalidOperationException("Post-processing error");
            });
            
            var logger = serviceProvider.GetRequiredService<ILogger<GlobalExceptionHandlerMiddleware>>();
            var middleware = new GlobalExceptionHandlerMiddleware(
                async (ctx) => { throw new Exception("Test exception"); },
                logger,
                options
            );

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            // Should not throw, but complete successfully
            context.Response.StatusCode.Should().Be(500);
            var responseBody = ReadResponseBody(context);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody, jsonOptions);
            
            errorResponse.Should().NotBeNull();
            Regex.IsMatch(errorResponse!.ErrorId, @"^\d{6}$").Should().BeTrue();
        }

        private static string ReadResponseBody(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            return reader.ReadToEnd();
        }

        // Test custom exception handlers
        private class TestCustomExceptionHandler : ICustomExceptionHandler
        {
            public int Priority => 10;

            public bool CanHandle(Type exceptionType)
            {
                return exceptionType == typeof(ArgumentException);
            }

            public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
            {
                return new ExceptionResponse
                {
                    StatusCode = 422,
                    Message = "Custom validation error",
                    ExceptionType = "ArgumentException"
                };
            }
        }

        private class HighPriorityTestHandler : ICustomExceptionHandler
        {
            public int Priority => 5;

            public bool CanHandle(Type exceptionType)
            {
                return exceptionType == typeof(ArgumentException);
            }

            public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
            {
                return new ExceptionResponse
                {
                    StatusCode = 422,
                    Message = "High priority error",
                    ExceptionType = "ArgumentException"
                };
            }
        }

        private class LowPriorityTestHandler : ICustomExceptionHandler
        {
            public int Priority => 15;

            public bool CanHandle(Type exceptionType)
            {
                return exceptionType == typeof(ArgumentException);
            }

            public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
            {
                return new ExceptionResponse
                {
                    StatusCode = 400,
                    Message = "Low priority error",
                    ExceptionType = "ArgumentException"
                };
            }
        }

        private class ContextAwareTestHandler : ICustomExceptionHandler
        {
            public int Priority => 10;

            public bool CanHandle(Type exceptionType)
            {
                return exceptionType == typeof(ArgumentException);
            }

            public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
            {
                return new ExceptionResponse
                {
                    StatusCode = 422,
                    Message = "Context aware error",
                    ExceptionType = "ArgumentException",
                    Context = new Dictionary<string, object> { ["handlerContext"] = "testValue" }
                };
            }
        }
    }
} 