using DfE.CoreLibs.Http.Configuration;
using DfE.CoreLibs.Http.Interfaces;
using DfE.CoreLibs.Http.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;

namespace DfE.CoreLibs.Http.Tests.Helpers
{
    /// <summary>
    /// Helper class for testing the exception handler middleware.
    /// </summary>
    public static class ExceptionHandlerTestHelpers
    {
        /// <summary>
        /// Creates a test service collection with basic logging.
        /// </summary>
        /// <returns>The service collection.</returns>
        public static IServiceCollection CreateTestServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            return services;
        }

        /// <summary>
        /// Creates a test HTTP context with a memory stream for the response body.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>The HTTP context.</returns>
        public static HttpContext CreateTestHttpContext(IServiceProvider serviceProvider)
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = serviceProvider;
            return context;
        }

        /// <summary>
        /// Creates a test HTTP context with correlation ID.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <returns>The HTTP context.</returns>
        public static HttpContext CreateTestHttpContextWithCorrelationId(IServiceProvider serviceProvider, Guid correlationId)
        {
            var context = CreateTestHttpContext(serviceProvider);
            
            var correlationContext = Substitute.For<ICorrelationContext>();
            correlationContext.CorrelationId.Returns(correlationId);
            
            var mockServiceProvider = Substitute.For<IServiceProvider>();
            mockServiceProvider.GetService(typeof(ICorrelationContext)).Returns(correlationContext);
            context.RequestServices = mockServiceProvider;
            
            return context;
        }

        /// <summary>
        /// Creates a test application builder with the specified service provider.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder CreateTestApplicationBuilder(IServiceProvider serviceProvider)
        {
            var app = Substitute.For<IApplicationBuilder>();
            app.ApplicationServices.Returns(serviceProvider);
            return app;
        }

        /// <summary>
        /// Creates a request delegate that throws the specified exception.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        /// <returns>The request delegate.</returns>
        public static RequestDelegate CreateExceptionThrowingDelegate(Exception exception)
        {
            return async (ctx) =>
            {
                throw exception;
            };
        }



        /// <summary>
        /// Reads the response body from an HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>The response body as a string.</returns>
        public static string ReadResponseBody(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Deserializes the response body as an ExceptionResponse.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>The deserialized exception response.</returns>
        public static ExceptionResponse? DeserializeResponse(HttpContext context)
        {
            var responseBody = ReadResponseBody(context);
            return JsonSerializer.Deserialize<ExceptionResponse>(responseBody);
        }

        /// <summary>
        /// Creates a test custom exception handler.
        /// </summary>
        /// <param name="priority">The priority of the handler.</param>
        /// <param name="canHandle">Function to determine if the handler can handle an exception type.</param>
        /// <param name="handle">Function to handle the exception.</param>
        /// <returns>The custom exception handler.</returns>
        public static ICustomExceptionHandler CreateTestHandler(
            int priority,
            Func<Type, bool> canHandle,
            Func<Exception, Dictionary<string, object>?, (int, string)> handle)
        {
            return new TestCustomExceptionHandler(priority, canHandle, handle);
        }

        /// <summary>
        /// Creates a test exception handler that handles a specific exception type.
        /// </summary>
        /// <typeparam name="TException">The exception type to handle.</typeparam>
        /// <param name="priority">The priority of the handler.</param>
        /// <param name="statusCode">The status code to return.</param>
        /// <param name="message">The message to return.</param>
        /// <returns>The custom exception handler.</returns>
        public static ICustomExceptionHandler CreateTestHandler<TException>(
            int priority,
            int statusCode,
            string message) where TException : Exception
        {
            return CreateTestHandler(
                priority,
                exceptionType => exceptionType == typeof(TException),
                (exception, context) => (statusCode, message));
        }

        /// <summary>
        /// Asserts that the response has the expected properties.
        /// </summary>
        /// <param name="response">The exception response.</param>
        /// <param name="expectedStatusCode">The expected status code.</param>
        /// <param name="expectedMessage">The expected message.</param>
        /// <param name="expectedErrorIdPattern">The expected error ID pattern.</param>
        public static void AssertResponse(
            ExceptionResponse response,
            int expectedStatusCode,
            string expectedMessage,
            string expectedErrorIdPattern = @"^\d{6}$")
        {
            response.Should().NotBeNull();
            response!.StatusCode.Should().Be(expectedStatusCode);
            response.Message.Should().Be(expectedMessage);
            response.ErrorId.Should().Match(expectedErrorIdPattern);
            response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Asserts that the HTTP context has the expected response properties.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="expectedStatusCode">The expected status code.</param>
        /// <param name="expectedContentType">The expected content type.</param>
        public static void AssertHttpContext(
            HttpContext context,
            int expectedStatusCode,
            string expectedContentType = "application/json")
        {
            context.Response.StatusCode.Should().Be(expectedStatusCode);
            context.Response.ContentType.Should().Be(expectedContentType);
        }

        /// <summary>
        /// Test custom exception handler implementation.
        /// </summary>
        private class TestCustomExceptionHandler : ICustomExceptionHandler
        {
            private readonly int _priority;
            private readonly Func<Type, bool> _canHandle;
            private readonly Func<Exception, Dictionary<string, object>?, (int, string)> _handle;

            public TestCustomExceptionHandler(
                int priority,
                Func<Type, bool> canHandle,
                Func<Exception, Dictionary<string, object>?, (int, string)> handle)
            {
                _priority = priority;
                _canHandle = canHandle;
                _handle = handle;
            }

            public int Priority => _priority;

            public bool CanHandle(Type exceptionType)
            {
                return _canHandle(exceptionType);
            }

            public (int statusCode, string message) Handle(Exception exception, Dictionary<string, object>? context = null)
            {
                return _handle(exception, context);
            }
        }
    }
} 