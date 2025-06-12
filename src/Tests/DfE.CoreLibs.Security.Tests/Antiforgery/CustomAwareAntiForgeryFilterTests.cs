using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; 
using NSubstitute;
using Microsoft.AspNetCore.Routing;
using DfE.CoreLibs.Security.Antiforgery; 

namespace DfE.CoreLibs.Security.Tests.Antiforgery
{
    public class CustomAwareAntiForgeryFilterTests
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

        [Theory]
        [InlineData("GET")]
        [InlineData("OPTIONS")]
        [InlineData("TRACE")]
        [InlineData("HEAD")]
        public async Task OnAuthorizationAsync_Skips_OnSafeHttpMethods(string methodName)
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            var logger = Substitute.For<ILogger<CustomAwareAntiForgeryFilter>>(); 
            var customRequestCheckers = new List<ICustomRequestChecker>
            {
                Substitute.For<ICustomRequestChecker>(),
                Substitute.For<ICustomRequestChecker>()
            };
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, customRequestCheckers, logger);
            var context = CreateAuthorizationFilterContext(methodName);

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.DidNotReceive().ValidateRequestAsync(context.HttpContext);
        }
        [Fact]
        public async Task OnAuthorizationAsync_Skips_ForNonCustomUnsafeRequestWithEmptyCheckersList()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
            var logger = Substitute.For<ILogger<CustomAwareAntiForgeryFilter>>();
            var customRequestCheckers = new List<ICustomRequestChecker>();
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, customRequestCheckers, logger);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.Received().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("Enforcing anti-forgery for the request.");
        }

        [Fact]
        public async Task OnAuthorizationAsync_Enforce_ForNonCustomUnsafeRequestWithOneCheckerFails()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
            var logger = Substitute.For<ILogger<CustomAwareAntiForgeryFilter>>();
            var customRequestChecker = Substitute.For<ICustomRequestChecker>();
            customRequestChecker.IsValidRequest(Arg.Any<HttpContext>()).Returns(false);
            var cypressRequestChecker = Substitute.For<ICustomRequestChecker>();
            cypressRequestChecker.IsValidRequest(Arg.Any<HttpContext>()).Returns(true);
            var customRequestCheckers = new List<ICustomRequestChecker>
            {
                customRequestChecker,
                cypressRequestChecker
            };
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, customRequestCheckers, logger);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.Received().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("Enforcing anti-forgery for the request.");
        }
        [Fact]
        public async Task OnAuthorizationAsync_Skips_ForNonCustomUnsafeRequestWithAllPassesCheckers()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
            var logger = Substitute.For<ILogger<CustomAwareAntiForgeryFilter>>();
            var customRequestChecker = Substitute.For<ICustomRequestChecker>();
            customRequestChecker.IsValidRequest(Arg.Any<HttpContext>()).Returns(true);
            var cypressRequestChecker = Substitute.For<ICustomRequestChecker>();
            cypressRequestChecker.IsValidRequest(Arg.Any<HttpContext>()).Returns(true);
            var customRequestCheckers = new List<ICustomRequestChecker>
            {
                customRequestChecker,
                cypressRequestChecker
            };
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, customRequestCheckers, logger);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.DidNotReceive().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("Skipping anti-forgery for the request due to matching all conditions.");
        }
    }
}
