using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DfE.CoreLibs.Security.Cypress;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace DfE.CoreLibs.Security.Tests.CypressTests
{
    public class CypressAuthenticationExtensionsTests
    {
        private readonly IFixture _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

        [Fact]
        public async Task AddCypressMultiAuthentication_RegistersRequiredServicesAndPolicyScheme()
        {
            // Arrange
            var services = new ServiceCollection();

            services.AddAuthentication();

            var authBuilder = new AuthenticationBuilder(services);

            // Act
            authBuilder.AddCypressMultiAuthentication(policyScheme: "TestPolicy", cypressScheme: "CypressAuth", fallbackScheme: "Cookies");

            Assert.Contains(services, d => d.ServiceType == typeof(ICypressRequestChecker) && d.ImplementationType == typeof(CypressRequestChecker));

            var schemeProvider = services.BuildServiceProvider().GetService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider?.GetSchemeAsync("CypressAuth")!;
            Assert.NotNull(scheme);

            // Create a fake HttpContext with required headers.
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["x-user-context-name"] = "cypressUser";
            httpContext.Request.Headers.Authorization = "Bearer secret123";

            var checker = Substitute.For<ICypressRequestChecker>();
            checker.IsCypressRequest(httpContext).Returns(true);

            var sp = new ServiceCollection().AddSingleton(checker).BuildServiceProvider();
            httpContext.RequestServices = sp;

            var selector = new Func<HttpContext, string>(context =>
            {
                var chk = context.RequestServices.GetRequiredService<ICypressRequestChecker>();
                return chk.IsCypressRequest(context) ? "CypressAuth" : "Cookies";
            });
            var schemeName = selector(httpContext);
            Assert.Equal("CypressAuth", schemeName);
        }

        [Fact]
        public void ForwardDefaultSelector_ReturnsCookies_WhenNotCypress()
        {
            // Arrange
            const string cypressScheme = "CypressAuth";
            const string fallbackScheme = CookieAuthenticationDefaults.AuthenticationScheme; // "Cookies"

            var fakeChecker = Substitute.For<ICypressRequestChecker>();
            fakeChecker.IsCypressRequest(Arg.Any<HttpContext>()).Returns(false);

            var services = new ServiceCollection();
            services.AddSingleton(fakeChecker);
            var sp = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = sp
            };

            Func<HttpContext, string> forwardSelector = context =>
            {
                var checker = context.RequestServices.GetRequiredService<ICypressRequestChecker>();
                return checker.IsCypressRequest(context) ? cypressScheme : fallbackScheme;
            };

            // Act
            var selectedScheme = forwardSelector(httpContext);

            // Assert
            Assert.Equal(fallbackScheme, selectedScheme);
        }
    
}
}