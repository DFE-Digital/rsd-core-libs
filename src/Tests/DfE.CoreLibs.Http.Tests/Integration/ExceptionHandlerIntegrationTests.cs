using DfE.CoreLibs.Http.Configuration;
using DfE.CoreLibs.Http.Extensions;
using DfE.CoreLibs.Http.Interfaces;
using DfE.CoreLibs.Http.Models;
using DfE.CoreLibs.Http.Utils;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;
using Xunit;

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

            var app = Substitute.For<IApplicationBuilder>();
            app.ApplicationServices.Returns(serviceProvider);

            var middleware = app.UseGlobalExceptionHandler();

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            var nextCalled = false;
            RequestDelegate next = async (ctx) =>
            {
                nextCalled = true;
                throw new ArgumentException("Test exception");
            };

            // Act
            await next(context);

            // Assert
            nextCalled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(500);
            context.Response.ContentType.Should().Be("application/json");

            var responseBody = ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.ErrorId.Should().Match(@"^\d{6}$");
            errorResponse.StatusCode.Should().Be(500);
            errorResponse.Message.Should().Be("An unexpected error occurred");
        }

        [Fact]
        public async Task Middleware_ShouldHandleCustomExceptionHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddCustomExceptionHandler<TestCustomExceptionHandler>();
            var serviceProvider = services.BuildServiceProvider();

            var app = Substitute.For<IApplicationBuilder>();
            app.ApplicationServices.Returns(serviceProvider);

            var middleware = app.UseGlobalExceptionHandler();

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            RequestDelegate next = async (ctx) =>
            {
                throw new ArgumentException("Test exception");
            };

            // Act
            await next(context);

            // Assert
            context.Response.StatusCode.Should().Be(422);
            var responseBody = ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody);
            
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

            var app = Substitute.For<IApplicationBuilder>();
            app.ApplicationServices.Returns(serviceProvider);

            var customErrorId = "CUSTOM-123";
            var middleware = app.UseGlobalExceptionHandler(options =>
            {
                options.WithCustomErrorIdGenerator(() => customErrorId);
            });

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            RequestDelegate next = async (ctx) =>
            {
                throw new Exception("Test exception");
            };

            // Act
            await next(context);

            // Assert
            var responseBody = ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody);
            
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

            var app = Substitute.For<IApplicationBuilder>();
            app.ApplicationServices.Returns(serviceProvider);

            var middleware = app.UseGlobalExceptionHandler(options =>
            {
                options.WithEnvironmentAwareErrorIds("Development");
            });

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            RequestDelegate next = async (ctx) =>
            {
                throw new Exception("Test exception");
            };

            // Act
            await next(context);

            // Assert
            var responseBody = ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.ErrorId.Should().Match(@"^D-\d{6}$");
        }

        [Fact]
        public async Task Middleware_ShouldExecuteSharedPostProcessing()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var app = Substitute.For<IApplicationBuilder>();
            app.ApplicationServices.Returns(serviceProvider);

            var postProcessingCalled = false;
            var middleware = app.UseGlobalExceptionHandler(options =>
            {
                options.WithSharedPostProcessing((exception, response) =>
                {
                    postProcessingCalled = true;
                    response.Context = new Dictionary<string, object> { ["processed"] = true };
                });
            });

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            RequestDelegate next = async (ctx) =>
            {
                throw new Exception("Test exception");
            };

            // Act
            await next(context);

            // Assert
            postProcessingCalled.Should().BeTrue();
            var responseBody = ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody);
            
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

            var app = Substitute.For<IApplicationBuilder>();
            app.ApplicationServices.Returns(serviceProvider);

            var middleware = app.UseGlobalExceptionHandler();

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

            RequestDelegate next = async (ctx) =>
            {
                throw new Exception("Test exception");
            };

            // Act
            await next(context);

            // Assert
            var responseBody = ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.CorrelationId.Should().Be(correlationId.ToString());
        }

        [Fact]
        public async Task Middleware_ShouldHandleMultipleCustomHandlers_WithPriority()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddCustomExceptionHandlers(
                new LowPriorityTestHandler(),
                new HighPriorityTestHandler()
            );
            var serviceProvider = services.BuildServiceProvider();

            var app = Substitute.For<IApplicationBuilder>();
            app.ApplicationServices.Returns(serviceProvider);

            var middleware = app.UseGlobalExceptionHandler();

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            RequestDelegate next = async (ctx) =>
            {
                throw new ArgumentException("Test exception");
            };

            // Act
            await next(context);

            // Assert
            context.Response.StatusCode.Should().Be(422); // High priority handler should be used
            var responseBody = ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody);
            
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

            var app = Substitute.For<IApplicationBuilder>();
            app.ApplicationServices.Returns(serviceProvider);

            var middleware = app.UseGlobalExceptionHandler(options =>
            {
                options.IgnoredExceptionTypes.Add(typeof(ArgumentException));
            });

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            RequestDelegate next = async (ctx) =>
            {
                throw new ArgumentException("Test exception");
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => next(context));
            exception.Message.Should().Be("Test exception");
        }

        [Fact]
        public async Task Middleware_ShouldHandleContextInCustomHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddCustomExceptionHandler<ContextAwareTestHandler>();
            var serviceProvider = services.BuildServiceProvider();

            var app = Substitute.For<IApplicationBuilder>();
            app.ApplicationServices.Returns(serviceProvider);

            var middleware = app.UseGlobalExceptionHandler();

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            RequestDelegate next = async (ctx) =>
            {
                throw new ArgumentException("Test exception");
            };

            // Act
            await next(context);

            // Assert
            var responseBody = ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.Context.Should().ContainKey("handlerContext");
            errorResponse.Context!["handlerContext"].Should().Be("testValue");
        }

        [Fact]
        public async Task Middleware_ShouldHandlePostProcessingExceptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var app = Substitute.For<IApplicationBuilder>();
            app.ApplicationServices.Returns(serviceProvider);

            var middleware = app.UseGlobalExceptionHandler(options =>
            {
                options.WithSharedPostProcessing((exception, response) =>
                {
                    throw new InvalidOperationException("Post-processing error");
                });
            });

            // Create a simple pipeline
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;

            RequestDelegate next = async (ctx) =>
            {
                throw new Exception("Test exception");
            };

            // Act
            await next(context);

            // Assert
            // Should not throw, but complete successfully
            context.Response.StatusCode.Should().Be(500);
            var responseBody = ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(responseBody);
            
            errorResponse.Should().NotBeNull();
            errorResponse!.ErrorId.Should().Match(@"^\d{6}$");
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

            public (int statusCode, string message) Handle(Exception exception, Dictionary<string, object>? context = null)
            {
                return (422, "Custom validation error");
            }
        }

        private class HighPriorityTestHandler : ICustomExceptionHandler
        {
            public int Priority => 5;

            public bool CanHandle(Type exceptionType)
            {
                return exceptionType == typeof(ArgumentException);
            }

            public (int statusCode, string message) Handle(Exception exception, Dictionary<string, object>? context = null)
            {
                return (422, "High priority error");
            }
        }

        private class LowPriorityTestHandler : ICustomExceptionHandler
        {
            public int Priority => 15;

            public bool CanHandle(Type exceptionType)
            {
                return exceptionType == typeof(ArgumentException);
            }

            public (int statusCode, string message) Handle(Exception exception, Dictionary<string, object>? context = null)
            {
                return (400, "Low priority error");
            }
        }

        private class ContextAwareTestHandler : ICustomExceptionHandler
        {
            public int Priority => 10;

            public bool CanHandle(Type exceptionType)
            {
                return exceptionType == typeof(ArgumentException);
            }

            public (int statusCode, string message) Handle(Exception exception, Dictionary<string, object>? context = null)
            {
                if (context != null)
                {
                    context["handlerContext"] = "testValue";
                }
                return (422, "Context aware error");
            }
        }
    }
} 