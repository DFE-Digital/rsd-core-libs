using DfE.CoreLibs.Security.Antiforgery;
using DfE.CoreLibs.Security.Cypress;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DfE.CoreLibs.Security.Tests.CypressTests
{
    public class CypressAwareAntiForgeryFilterTests
    {
        private static AuthorizationFilterContext CreateAuthorizationFilterContext(string method, string path = "/test")
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = method;
            httpContext.Request.Path = path;

            var routeData = new RouteData();
            var actionDescriptor = new ActionDescriptor();
            var modelState = new ModelStateDictionary();

            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor, modelState);
            return new AuthorizationFilterContext(actionContext, []);
        }

        [Fact]
        public async Task OnAuthorizationAsync_Skips_When_ShouldSkipAntiforgeryPredicateReturnsTrue()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            var logger = Substitute.For<ILogger<CypressAwareAntiForgeryFilter>>();
            var cypressChecker = Substitute.For<ICustomRequestChecker>();
            var options = Options.Create(new CustomAntiForgeryOptions
            {
                ShouldSkipAntiforgery = _ => true
            });
            var filter = new CypressAwareAntiForgeryFilter(antiforgery, logger, cypressChecker, options);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.DidNotReceive().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("Skipping anti-forgery due to ShouldSkipAntiforgery predicate.");
        }

        [Fact]
        public async Task OnAuthorizationAsync_Skips_OnSafeHttpMethods()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            var logger = Substitute.For<ILogger<CypressAwareAntiForgeryFilter>>();
            var cypressChecker = Substitute.For<ICustomRequestChecker>();
            var options = Options.Create(new CustomAntiForgeryOptions
            {
                ShouldSkipAntiforgery = _ => false
            });
            var filter = new CypressAwareAntiForgeryFilter(antiforgery, logger, cypressChecker, options);
            var context = CreateAuthorizationFilterContext("GET");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.DidNotReceive().ValidateRequestAsync(context.HttpContext);
        }

        [Fact]
        public async Task OnAuthorizationAsync_Skips_ForCypressRequest()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            var logger = Substitute.For<ILogger<CypressAwareAntiForgeryFilter>>();
            var cypressChecker = Substitute.For<ICustomRequestChecker>();
            cypressChecker.IsValidRequest(Arg.Any<HttpContext>()).Returns(true);
            var options = Options.Create(new CustomAntiForgeryOptions
            {
                ShouldSkipAntiforgery = _ => false
            });
            var filter = new CypressAwareAntiForgeryFilter(antiforgery, logger, cypressChecker, options);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.DidNotReceive().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("Skipping anti-forgery for Cypress request.");
        }

        [Fact]
        public async Task OnAuthorizationAsync_EnforcesAntiforgery_ForNonCypressUnsafeRequest()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
            var logger = Substitute.For<ILogger<CypressAwareAntiForgeryFilter>>();
            var cypressChecker = Substitute.For<ICustomRequestChecker>();
            cypressChecker.IsValidRequest(Arg.Any<HttpContext>()).Returns(false);
            var options = Options.Create(new CustomAntiForgeryOptions
            {
                ShouldSkipAntiforgery = _ => false
            });
            var filter = new CypressAwareAntiForgeryFilter(antiforgery, logger, cypressChecker, options);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.Received().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("Enforcing anti-forgery for non-Cypress request.");
        }
    }
}
