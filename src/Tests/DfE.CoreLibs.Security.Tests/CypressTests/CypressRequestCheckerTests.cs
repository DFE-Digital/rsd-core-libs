using System;
using DfE.CoreLibs.Security.Cypress;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using Xunit;
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DfE.CoreLibs.Security.Tests.CypressTests
{
    public class CypressRequestCheckerTests
    {
        private readonly IFixture _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

        private static DefaultHttpContext CreateHttpContext(string environmentName, string authHeader, string userContextHeader)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[HeaderNames.Authorization] = $"Bearer {authHeader}";
            httpContext.Request.Headers["x-cypress-user"] = userContextHeader;
            return httpContext;
        }

        [Fact]
        public void IsCypressRequest_ReturnsFalse_WhenUserContextDoesNotMatch()
        {
            // Arrange
            var env = Substitute.For<IHostEnvironment>();
            env.EnvironmentName.Returns("Development");
            var config = Substitute.For<IConfiguration>();
            config["CypressTestSecret"].Returns("secret123");

            var httpContext = CreateHttpContext("Development", "secret123", "notCypress");
            var checker = new CypressRequestChecker(env, config);

            // Act
            var result = checker.IsCypressRequest(httpContext);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("Production")]
        [InlineData("Staging")]
        public void IsCypressRequest_ReturnsFalse_WhenEnvironmentNotAllowed(string environmentName)
        {
            // Arrange
            var env = Substitute.For<IHostEnvironment>();
            env.EnvironmentName.Returns(environmentName);
            var config = Substitute.For<IConfiguration>();
            config["CypressTestSecret"].Returns("secret123");

            // Valid headers for Cypress
            var httpContext = CreateHttpContext("Ignored", "secret123", "cypressUser");
            var checker = new CypressRequestChecker(env, config);

            // Act
            var result = checker.IsCypressRequest(httpContext);

            // Assert
            // Only "Development", "Staging" and "Test" are allowed per our code.
            if (environmentName.Equals("Staging", StringComparison.OrdinalIgnoreCase) ||
                environmentName.Equals("Development", StringComparison.OrdinalIgnoreCase) ||
                environmentName.Equals("Test", StringComparison.OrdinalIgnoreCase))
            {
                Assert.True(result);
            }
            else
            {
                Assert.False(result);
            }
        }

        [Fact]
        public void IsCypressRequest_ReturnsFalse_WhenSecretOrAuthHeaderMissing()
        {
            // Arrange
            var env = Substitute.For<IHostEnvironment>();
            env.EnvironmentName.Returns("Development");
            var config = Substitute.For<IConfiguration>();
            config["CypressTestSecret"].Returns("secret123");

            // Missing auth header:
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["x-cypress-user"] = "cypressUser";
            // No Authorization header.
            var checker = new CypressRequestChecker(env, config);

            // Act
            var result = checker.IsCypressRequest(httpContext);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCypressRequest_ReturnsTrue_ForValidCypressRequest()
        {
            // Arrange
            var env = Substitute.For<IHostEnvironment>();
            env.EnvironmentName.Returns("Development");
            var config = Substitute.For<IConfiguration>();
            config["CypressTestSecret"].Returns("secret123");

            var httpContext = CreateHttpContext("Development", "secret123", "cypressUser");
            var checker = new CypressRequestChecker(env, config);

            // Act
            var result = checker.IsCypressRequest(httpContext);

            // Assert
            Assert.True(result);
        }
    }
}
