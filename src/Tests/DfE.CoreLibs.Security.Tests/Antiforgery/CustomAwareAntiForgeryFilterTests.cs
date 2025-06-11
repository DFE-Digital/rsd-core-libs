using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        [Fact]
        public async Task OnAuthorizationAsync_Skips_When_ShouldSkipAntiforgeryPredicateReturnsTrue()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            var logger = Substitute.For<ILogger<CustomAwareAntiForgeryFilter>>();
            var customChecker = Substitute.For<ICustomRequestChecker>();
            var cypressChecker = Substitute.For<ICypressRequestChecker>();
            var options = Options.Create(new CustomAwareAntiForgeryOptions
            {
                ShouldSkipAntiforgery = _ => true,
                RequestHeaderKey = "X-Custom-Header"
            });
            var skipConditions = new List<Func<HttpContext, bool>>
            {
                ctx => customChecker.IsValidRequest(ctx, options.Value.RequestHeaderKey),
                cypressChecker.IsCypressRequest,
                options.Value.ShouldSkipAntiforgery
            };
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, logger, skipConditions, options);
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
            var logger = Substitute.For<ILogger<CustomAwareAntiForgeryFilter>>();
            var customChecker = Substitute.For<ICustomRequestChecker>();
            var cypressChecker = Substitute.For<ICypressRequestChecker>();
            var options = Options.Create(new CustomAwareAntiForgeryOptions
            {
                ShouldSkipAntiforgery = _ => false,
                RequestHeaderKey = "X-Custom-Header"
            });
            var skipConditions = new List<Func<HttpContext, bool>>
            {
                ctx => customChecker.IsValidRequest(ctx, options.Value.RequestHeaderKey),
                cypressChecker.IsCypressRequest,
                options.Value.ShouldSkipAntiforgery
            };
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, logger, skipConditions, options);
            var context = CreateAuthorizationFilterContext("GET");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.DidNotReceive().ValidateRequestAsync(context.HttpContext);
        }

        [Fact]
        public async Task OnAuthorizationAsync_Skips_ForCustomRequest()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            var logger = Substitute.For<ILogger<CustomAwareAntiForgeryFilter>>();
            var customChecker = Substitute.For<ICustomRequestChecker>();
            var cypressChecker = Substitute.For<ICypressRequestChecker>();

            var options = Options.Create(new CustomAwareAntiForgeryOptions
            {
                ShouldSkipAntiforgery = _ => false,
                RequestHeaderKey = "X-Custom-Header"
            });
            customChecker.IsValidRequest(Arg.Any<HttpContext>(), options.Value.RequestHeaderKey).Returns(true);
            var skipConditions = new List<Func<HttpContext, bool>>
            {
                ctx => customChecker.IsValidRequest(ctx, options.Value.RequestHeaderKey),
                cypressChecker.IsCypressRequest
            };
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, logger, skipConditions, options);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.DidNotReceive().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("Skipping anti-forgery for the request due to a matching condition.");
        }

        [Fact]
        public async Task OnAuthorizationAsync_EnforcesAntiforgery_ForNonCustomUnsafeRequest()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
            var logger = Substitute.For<ILogger<CustomAwareAntiForgeryFilter>>();
            var customChecker = Substitute.For<ICustomRequestChecker>();
            var cypressChecker = Substitute.For<ICypressRequestChecker>();
            customChecker.IsValidRequest(Arg.Any<HttpContext>(), Arg.Any<string?>()).Returns(false);
            cypressChecker.IsCypressRequest(Arg.Any<HttpContext>()).Returns(false);
            var options = Options.Create(new CustomAwareAntiForgeryOptions
            {
                ShouldSkipAntiforgery = _ => false
            });
            var skipConditions = new List<Func<HttpContext, bool>>
            {
                ctx => customChecker.IsValidRequest(ctx, options.Value.RequestHeaderKey),
                cypressChecker.IsCypressRequest,
                options.Value.ShouldSkipAntiforgery
            };
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, logger, skipConditions, options);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.Received().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("Enforcing anti-forgery for the request.");
        }
        [Fact]
        public async Task OnAuthorizationAsync_Skips_ForCypressRequest()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            var logger = Substitute.For<ILogger<CustomAwareAntiForgeryFilter>>();
            var customChecker = Substitute.For<ICustomRequestChecker>(); 
            var cypressChecker = Substitute.For<ICypressRequestChecker>();
            customChecker.IsValidRequest(Arg.Any<HttpContext>(), Arg.Any<string?>()).Returns(false);
            cypressChecker.IsCypressRequest(Arg.Any<HttpContext>()).Returns(true);
            var options = Options.Create(new CustomAwareAntiForgeryOptions
            {
                ShouldSkipAntiforgery = _ => false
            });
            var skipConditions = new List<Func<HttpContext, bool>>
            {
                ctx => customChecker.IsValidRequest(ctx, options.Value.RequestHeaderKey),
                cypressChecker.IsCypressRequest
            };
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, logger, skipConditions, options);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.DidNotReceive().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("Skipping anti-forgery for the request due to a matching condition.");
        }
    }
}
