using FluentAssertions;
using GovUK.Dfe.CoreLibs.Http.NoScriptDetection;
using GovUK.Dfe.CoreLibs.Http.NoScriptDetection.Pixel;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Customizations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using System.Reflection;

namespace GovUK.Dfe.CoreLibs.Http.Tests.NoScriptDetection
{
    public class NoScriptDetectionMiddlewareTests
    {
        private readonly RequestDelegate _nextDelegate;
        private readonly TelemetryClient _telemetryClient;
        private readonly TransparentPixelProvider _pixelProvider;
        private readonly ITelemetryChannel _telemetryChannel;

        public NoScriptDetectionMiddlewareTests()
        {
            _nextDelegate = Substitute.For<RequestDelegate>();
            _telemetryChannel = Substitute.For<ITelemetryChannel>();
            
            var configuration = new TelemetryConfiguration
            {
                TelemetryChannel = _telemetryChannel,
                ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
            };
            _telemetryClient = new TelemetryClient(configuration);
            
            // Use real implementation since interface is internal
            _pixelProvider = new TransparentPixelProvider();
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldCallNextDelegate_WhenPathDoesNotMatch(DefaultHttpContext context)
        {
            // Arrange
            context.Request.Path = "/some/other/path";
            context.Response.Body = new MemoryStream();
            _nextDelegate.Invoke(context).Returns(Task.CompletedTask);

            var middleware = CreateMiddleware();

            // Act
            await InvokeMiddlewareAsync(middleware, context, _telemetryClient, _pixelProvider);

            // Assert
            await _nextDelegate.Received(1).Invoke(context);
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldNotCallNextDelegate_WhenPathMatches(DefaultHttpContext context)
        {
            // Arrange
            context.Request.Path = "/_noscript/pixel";
            context.Response.Body = new MemoryStream();

            var middleware = CreateMiddleware();

            // Act
            await InvokeMiddlewareAsync(middleware, context, _telemetryClient, _pixelProvider);

            // Assert
            await _nextDelegate.DidNotReceive().Invoke(context);
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldSetContentTypeToImagePng_WhenPathMatches(DefaultHttpContext context)
        {
            // Arrange
            context.Request.Path = "/_noscript/pixel";
            context.Response.Body = new MemoryStream();

            var middleware = CreateMiddleware();

            // Act
            await InvokeMiddlewareAsync(middleware, context, _telemetryClient, _pixelProvider);

            // Assert
            context.Response.ContentType.Should().Be("image/png");
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldSetCacheControlHeaders_WhenPathMatches(DefaultHttpContext context)
        {
            // Arrange
            context.Request.Path = "/_noscript/pixel";
            context.Response.Body = new MemoryStream();

            var middleware = CreateMiddleware();

            // Act
            await InvokeMiddlewareAsync(middleware, context, _telemetryClient, _pixelProvider);

            // Assert
            var cacheControl = context.Response.GetTypedHeaders().CacheControl;
            cacheControl.Should().NotBeNull();
            cacheControl!.NoStore.Should().BeTrue();
            cacheControl.NoCache.Should().BeTrue();
            cacheControl.MustRevalidate.Should().BeTrue();
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldWritePixelToResponse_WhenPathMatches(DefaultHttpContext context)
        {
            // Arrange
            var expectedPixel = _pixelProvider.GetPixel();
            
            context.Request.Path = "/_noscript/pixel";
            context.Response.Body = new MemoryStream();

            var middleware = CreateMiddleware();

            // Act
            await InvokeMiddlewareAsync(middleware, context, _telemetryClient, _pixelProvider);

            // Assert
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBytes = new byte[expectedPixel.Length];
            await context.Response.Body.ReadAsync(responseBytes.AsMemory());
            responseBytes.Should().BeEquivalentTo(expectedPixel);
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldTrackTelemetryEvent_WhenPathMatches(DefaultHttpContext context)
        {
            // Arrange
            context.Request.Path = "/_noscript/pixel";
            context.Response.Body = new MemoryStream();

            var middleware = CreateMiddleware();

            // Act
            await InvokeMiddlewareAsync(middleware, context, _telemetryClient, _pixelProvider);

            // Assert
            // The telemetry client was invoked - we verify this indirectly through the fact that 
            // TelemetryClient.TrackEvent was called. Since TelemetryClient is not easily mockable,
            // we verify the channel received the telemetry.
            _telemetryChannel.Received().Send(Arg.Any<ITelemetry>());
        }

        [Theory]
        [InlineData("/_noscript/pixel")]
        [InlineData("/_NOSCRIPT/PIXEL")]
        [InlineData("/_NoScript/Pixel")]
        public async Task InvokeAsync_ShouldMatchPath_CaseInsensitively(string path)
        {
            // Arrange
            // PathString comparison in ASP.NET Core is case-insensitive
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Response.Body = new MemoryStream();

            var middleware = CreateMiddleware();

            // Act
            await InvokeMiddlewareAsync(middleware, context, _telemetryClient, _pixelProvider);

            // Assert
            // All paths should match since PathString comparison is case-insensitive
            context.Response.ContentType.Should().Be("image/png");
            await _nextDelegate.DidNotReceive().Invoke(context);
        }

        [Theory]
        [InlineData("/_noscript/pixel/extra")]
        [InlineData("/_noscript")]
        [InlineData("/noscript/pixel")]
        public async Task InvokeAsync_ShouldNotMatch_SimilarButDifferentPaths(string path)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Response.Body = new MemoryStream();
            _nextDelegate.Invoke(context).Returns(Task.CompletedTask);

            var middleware = CreateMiddleware();

            // Act
            await InvokeMiddlewareAsync(middleware, context, _telemetryClient, _pixelProvider);

            // Assert
            await _nextDelegate.Received(1).Invoke(context);
            context.Response.ContentType.Should().BeNull();
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldHandleEmptyPath(DefaultHttpContext context)
        {
            // Arrange
            context.Request.Path = "";
            context.Response.Body = new MemoryStream();
            _nextDelegate.Invoke(context).Returns(Task.CompletedTask);

            var middleware = CreateMiddleware();

            // Act
            await InvokeMiddlewareAsync(middleware, context, _telemetryClient, _pixelProvider);

            // Assert
            await _nextDelegate.Received(1).Invoke(context);
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldHandleRootPath(DefaultHttpContext context)
        {
            // Arrange
            context.Request.Path = "/";
            context.Response.Body = new MemoryStream();
            _nextDelegate.Invoke(context).Returns(Task.CompletedTask);

            var middleware = CreateMiddleware();

            // Act
            await InvokeMiddlewareAsync(middleware, context, _telemetryClient, _pixelProvider);

            // Assert
            await _nextDelegate.Received(1).Invoke(context);
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldNotSetContentType_WhenPathDoesNotMatch(DefaultHttpContext context)
        {
            // Arrange
            context.Request.Path = "/other/path";
            context.Response.Body = new MemoryStream();
            _nextDelegate.Invoke(context).Returns(Task.CompletedTask);

            var middleware = CreateMiddleware();

            // Act
            await InvokeMiddlewareAsync(middleware, context, _telemetryClient, _pixelProvider);

            // Assert
            context.Response.ContentType.Should().BeNull();
        }

        [Theory]
        [CustomAutoData(typeof(HttpContextCustomization))]
        public async Task InvokeAsync_ShouldWriteCorrectPngBytes_WhenPathMatches(DefaultHttpContext context)
        {
            // Arrange
            context.Request.Path = "/_noscript/pixel";
            context.Response.Body = new MemoryStream();

            var middleware = CreateMiddleware();

            // Act
            await InvokeMiddlewareAsync(middleware, context, _telemetryClient, _pixelProvider);

            // Assert
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[4];
            await context.Response.Body.ReadAsync(buffer.AsMemory(0, 4));
            
            // Verify PNG magic bytes
            buffer[0].Should().Be(0x89);
            buffer[1].Should().Be(0x50); // 'P'
            buffer[2].Should().Be(0x4E); // 'N'
            buffer[3].Should().Be(0x47); // 'G'
        }

        [Fact]
        public void Middleware_ShouldBeInternalSealed()
        {
            // Arrange
            var middlewareType = GetMiddlewareType();

            // Assert
            middlewareType.Should().NotBeNull();
            middlewareType!.IsSealed.Should().BeTrue();
            middlewareType.IsNotPublic.Should().BeTrue();
        }

        [Fact]
        public void Middleware_ShouldHaveInvokeAsyncMethod()
        {
            // Arrange
            var middlewareType = GetMiddlewareType();

            // Act
            var invokeMethod = middlewareType?.GetMethod("InvokeAsync");

            // Assert
            invokeMethod.Should().NotBeNull();
            invokeMethod!.ReturnType.Should().Be(typeof(Task));
        }

        private object CreateMiddleware()
        {
            // Use reflection to instantiate the internal middleware
            var middlewareType = GetMiddlewareType();

            middlewareType.Should().NotBeNull("Middleware type should exist");

            var instance = Activator.CreateInstance(middlewareType!, _nextDelegate);
            instance.Should().NotBeNull("Middleware instance should be created");

            return instance!;
        }

        private static Type? GetMiddlewareType()
        {
            return typeof(NoScriptDetectionApplicationBuilderExtensions)
                .Assembly
                .GetType("GovUK.Dfe.CoreLibs.Http.NoScriptDetection.Middleware.NoScriptDetectionMiddleware");
        }

        private static async Task InvokeMiddlewareAsync(
            object middleware,
            HttpContext context,
            TelemetryClient telemetryClient,
            object pixelProvider)
        {
            var method = middleware.GetType().GetMethod("InvokeAsync");
            method.Should().NotBeNull("InvokeAsync method should exist");

            var task = (Task)method!.Invoke(middleware, new object[] { context, telemetryClient, pixelProvider })!;
            await task;
        }
    }
}
