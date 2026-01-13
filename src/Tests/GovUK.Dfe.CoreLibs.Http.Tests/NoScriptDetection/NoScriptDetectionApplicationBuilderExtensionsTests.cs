using FluentAssertions;
using GovUK.Dfe.CoreLibs.Http.NoScriptDetection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace GovUK.Dfe.CoreLibs.Http.Tests.NoScriptDetection
{
    public class NoScriptDetectionApplicationBuilderExtensionsTests
    {
        [Fact]
        public void UseNoScriptDetection_ShouldReturnApplicationBuilder()
        {
            // Arrange
            var app = Substitute.For<IApplicationBuilder>();
            app.Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>()).Returns(app);

            // Act
            var result = app.UseNoScriptDetection();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IApplicationBuilder>();
        }

        [Fact]
        public void UseNoScriptDetection_ShouldRegisterMiddleware()
        {
            // Arrange
            var app = Substitute.For<IApplicationBuilder>();
            Func<RequestDelegate, RequestDelegate>? capturedMiddleware = null;
            
            app.Use(Arg.Do<Func<RequestDelegate, RequestDelegate>>(m => capturedMiddleware = m))
               .Returns(app);

            // Act
            app.UseNoScriptDetection();

            // Assert
            app.Received(1).Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>());
            capturedMiddleware.Should().NotBeNull();
        }

        [Fact]
        public void UseNoScriptDetection_ShouldBeChainable()
        {
            // Arrange
            var app = Substitute.For<IApplicationBuilder>();
            app.Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>()).Returns(app);

            // Act
            var result = app.UseNoScriptDetection();

            // Assert
            result.Should().BeSameAs(app);
        }
    }
}

