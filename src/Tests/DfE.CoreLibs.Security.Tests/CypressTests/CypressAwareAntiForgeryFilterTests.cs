﻿using AutoFixture;
using AutoFixture.AutoNSubstitute;
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
        private readonly IFixture _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

        private AuthorizationFilterContext CreateAuthorizationFilterContext(string method, string path = "/test")
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = method;
            httpContext.Request.Path = path;

            var routeData = new RouteData();
            var actionDescriptor = new ActionDescriptor();
            var modelState = new ModelStateDictionary();

            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor, modelState);
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }

        [Fact]
        public async Task OnAuthorizationAsync_Skips_When_ShouldSkipAntiforgeryPredicateReturnsTrue()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            var logger = Substitute.For<ILogger<CypressAwareAntiForgeryFilter>>();
            var cypressChecker = Substitute.For<ICypressRequestChecker>();
            var options = Options.Create(new CypressAwareAntiForgeryOptions
            {
                ShouldSkipAntiforgery = _ => true
            });
            var filter = new CypressAwareAntiForgeryFilter(antiforgery, logger, cypressChecker, options);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.DidNotReceive().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("Skipping antiforgery due to ShouldSkipAntiforgery predicate.");
        }

        [Fact]
        public async Task OnAuthorizationAsync_Skips_OnSafeHttpMethods()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            var logger = Substitute.For<ILogger<CypressAwareAntiForgeryFilter>>();
            var cypressChecker = Substitute.For<ICypressRequestChecker>();
            var options = Options.Create(new CypressAwareAntiForgeryOptions
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
            var cypressChecker = Substitute.For<ICypressRequestChecker>();
            cypressChecker.IsCypressRequest(Arg.Any<HttpContext>()).Returns(true);
            var options = Options.Create(new CypressAwareAntiForgeryOptions
            {
                ShouldSkipAntiforgery = _ => false
            });
            var filter = new CypressAwareAntiForgeryFilter(antiforgery, logger, cypressChecker, options);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.DidNotReceive().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("Skipping antiforgery for Cypress request.");
        }

        [Fact]
        public async Task OnAuthorizationAsync_EnforcesAntiforgery_ForNonCypressUnsafeRequest()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
            var logger = Substitute.For<ILogger<CypressAwareAntiForgeryFilter>>();
            var cypressChecker = Substitute.For<ICypressRequestChecker>();
            cypressChecker.IsCypressRequest(Arg.Any<HttpContext>()).Returns(false);
            var options = Options.Create(new CypressAwareAntiForgeryOptions
            {
                ShouldSkipAntiforgery = _ => false
            });
            var filter = new CypressAwareAntiForgeryFilter(antiforgery, logger, cypressChecker, options);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.Received().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("Enforcing antiforgery for non-Cypress request.");
        }
    }
}
