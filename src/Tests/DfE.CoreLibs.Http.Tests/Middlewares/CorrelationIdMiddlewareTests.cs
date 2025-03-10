using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Http.Interfaces;
using DfE.CoreLibs.Http.Middlewares.CorrelationId;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.AutoFixture.Customizations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;

namespace DfE.CoreLibs.Http.Tests.Middlewares
{
    public class CorrelationIdMiddlewareTests
    {
        private readonly RequestDelegate _nextDelegate;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private readonly ICorrelationContext _correlationContext;
        private readonly CorrelationIdMiddleware _middleware;

        public CorrelationIdMiddlewareTests()
        {
            _nextDelegate = Substitute.For<RequestDelegate>();
            _logger = Substitute.For<ILogger<CorrelationIdMiddleware>>();
            _correlationContext = Substitute.For<ICorrelationContext>();
            _middleware = new CorrelationIdMiddleware(_nextDelegate, _logger);
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task Invoke_ShouldSetNewCorrelationId_WhenHeaderNotPresent(
            HttpContext context)
        {
            // Arrange
            context.Request.Headers.Remove(Keys.HeaderKey);
            context.Response.Body = new System.IO.MemoryStream();

            _nextDelegate.Invoke(context).Returns(Task.CompletedTask);

            // Act
            await _middleware.Invoke(context, _correlationContext);

            // Assert
            Assert.True(Guid.TryParse(context.Response.Headers[Keys.HeaderKey], out var correlationId));
            Assert.NotEqual(Guid.Empty, correlationId);
            _correlationContext.Received(1).SetContext(Arg.Any<Guid>());
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task Invoke_ShouldRetainExistingCorrelationId_WhenHeaderPresent([Frozen] IFixture fixture,
        DefaultHttpContext context)
        {
            // Arrange
            var existingCorrelationId = fixture.Create<Guid>();
            context.Request.Headers[Keys.HeaderKey] = existingCorrelationId.ToString();
            context.Response.Body = new System.IO.MemoryStream();

            _nextDelegate.Invoke(context).Returns(Task.CompletedTask);

            // Act
            await _middleware.Invoke(context, _correlationContext);

            // Assert
            Assert.Equal(existingCorrelationId.ToString(), context.Response.Headers[Keys.HeaderKey]);
            _correlationContext.Received(1).SetContext(existingCorrelationId);
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task Invoke_ShouldReturnBadRequest_WhenCorrelationIdIsEmpty(DefaultHttpContext context)
        {
            // Arrange
            context.Request.Headers[Keys.HeaderKey] = Guid.Empty.ToString();
            context.Response.Body = new System.IO.MemoryStream();

            _nextDelegate.Invoke(context).Returns(Task.CompletedTask);

            // Act
            await _middleware.Invoke(context, _correlationContext);

            // Assert
            Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
            Assert.Contains("Bad Request", ReadResponseBody(context));
            _correlationContext.DidNotReceive().SetContext(Arg.Any<Guid>());
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task Invoke_ShouldLogNewGuidWarning_WhenHeaderCannotBeParsed(DefaultHttpContext context)
        {
            // Arrange
            context.Request.Headers[Keys.HeaderKey] = "invalid-guid";
            context.Response.Body = new System.IO.MemoryStream();

            _nextDelegate.Invoke(context).Returns(Task.CompletedTask);

            // Act
            await _middleware.Invoke(context, _correlationContext);

            // Assert
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString().Contains("Detected header x-correlationId, but value cannot be parsed to a GUID")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);

            _correlationContext.Received(1).SetContext(Arg.Any<Guid>());
        }

        private static string ReadResponseBody(HttpContext context)
        {
            context.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
            using var reader = new System.IO.StreamReader(context.Response.Body);
            return reader.ReadToEnd();
        }
    }
}
