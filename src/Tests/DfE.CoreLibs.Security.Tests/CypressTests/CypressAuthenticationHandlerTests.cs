using DfE.CoreLibs.Security.Cypress;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Security.Claims;

namespace DfE.CoreLibs.Security.Tests.CypressTests
{
    public class CypressAuthenticationHandlerTests
    {
        [Fact]
        public async Task HandleAuthenticateAsync_ReturnsFail_WhenHttpContextIsNull()
        {
            // Arrange
            var optionsMonitor = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
            optionsMonitor.Get(Arg.Any<string>()).Returns(new AuthenticationSchemeOptions());
            var loggerFactory = Substitute.For<ILoggerFactory>();
            var encoder = Substitute.For<System.Text.Encodings.Web.UrlEncoder>();
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns((HttpContext?)null);

            var handler = new CypressAuthenticationHandler(optionsMonitor, loggerFactory, encoder, httpContextAccessor);

            // Act
            var result = await handler.CallBaseHandleAuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("No HttpContext", result?.Failure?.Message);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_ReturnsSuccess_WithValidHeaders()
        {
            // Arrange
            var optionsMonitor = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
            optionsMonitor.Get(Arg.Any<string>()).Returns(new AuthenticationSchemeOptions());
            var loggerFactory = Substitute.For<ILoggerFactory>();
            var encoder = System.Text.Encodings.Web.UrlEncoder.Default;

            var httpContext = new DefaultHttpContext();

            httpContext.Request.Headers["x-user-context-id"] = "test-id";
            httpContext.Request.Headers["x-user-context-name"] = "cypressUser";
            httpContext.Request.Headers["x-user-context-role-0"] = "testRole";
            httpContext.Request.Headers["Authorization"] = "Bearer secret123";

            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns(httpContext);

            var handler = new CypressAuthenticationHandler(optionsMonitor, loggerFactory, encoder, httpContextAccessor);

            var scheme = new AuthenticationScheme("CypressAuth", "CypressAuth", typeof(CypressAuthenticationHandler));
            await handler.InitializeAsync(scheme, httpContext);

            var result = await handler.CallBaseHandleAuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded);
            var principal = result.Principal;
            Assert.NotNull(principal);
            var identity = principal.Identity as ClaimsIdentity;
            Assert.NotNull(identity);

            Assert.Contains(identity.Claims, c => c.Type == ClaimTypes.Name && c.Value == "cypressUser");
            Assert.Contains(identity.Claims, c => c.Type == ClaimTypes.Role && c.Value == "testRole");
        }

    }

    // Helper extension method to call protected HandleAuthenticateAsync.
    public static class CypressAuthenticationHandlerTestExtensions
    {
        public static Task<AuthenticateResult> CallBaseHandleAuthenticateAsync(this CypressAuthenticationHandler handler)
        {
            // Use reflection to call the protected method.
            var method = typeof(CypressAuthenticationHandler).GetMethod("HandleAuthenticateAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return ((Task<AuthenticateResult>)method?.Invoke(handler, null)!);
        }
    }
}
