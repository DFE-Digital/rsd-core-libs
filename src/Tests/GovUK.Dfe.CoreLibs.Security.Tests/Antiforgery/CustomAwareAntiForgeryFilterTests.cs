using GovUK.Dfe.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; 
using NSubstitute;
using Microsoft.AspNetCore.Routing;
using GovUK.Dfe.CoreLibs.Security.Antiforgery;
using Microsoft.Extensions.Options;
using GovUK.Dfe.CoreLibs.Security.Enums;

namespace GovUK.Dfe.CoreLibs.Security.Tests.Antiforgery
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
            var options = Substitute.For<IOptions<CustomAwareAntiForgeryOptions>>();
            options.Value.Returns(new CustomAwareAntiForgeryOptions
            {
                CheckerGroups = []
            });
            var customRequestCheckers = new List<ICustomRequestChecker>
            {
                Substitute.For<ICustomRequestChecker>(),
                Substitute.For<ICustomRequestChecker>()
            };
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, customRequestCheckers, options, logger);
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
            var options = Substitute.For<IOptions<CustomAwareAntiForgeryOptions>>();
            options.Value.Returns(new CustomAwareAntiForgeryOptions
            {
                CheckerGroups = [
                    new CheckerGroup
                    {
                        TypeNames = [],
                        CheckerOperator = CheckerOperator.Or
                    }
                ],
            });
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, customRequestCheckers, options, logger);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.Received().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("Enforcing anti-forgery for the request.");
        }

        [Fact]
        public async Task OnAuthorizationAsync_Enforce_ForNonCustomUnsafeRequestWithOneCheckerFailsWithOrOperator()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
            var options = Substitute.For<IOptions<CustomAwareAntiForgeryOptions>>();
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
            options.Value.Returns(new CustomAwareAntiForgeryOptions
            {
                CheckerGroups =
                [
                    new CheckerGroup
                    {
                       TypeNames = customRequestCheckers.Select(c => c.GetType().Name).ToArray(),
                       CheckerOperator = CheckerOperator.Or
                    }
                ]
            });
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, customRequestCheckers, options, logger);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.DidNotReceive().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("All groups passed > skipping antiforgery.");
        }
        [Fact]
        public async Task OnAuthorizationAsync_Enforce_ForNonCustomUnsafeRequestWithOneCheckerFails()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
            var options = Substitute.For<IOptions<CustomAwareAntiForgeryOptions>>();
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
            options.Value.Returns(new CustomAwareAntiForgeryOptions
            {
                CheckerGroups =
                [
                    new CheckerGroup
                    {
                       TypeNames = customRequestCheckers.Select(c => c.GetType().Name).ToArray(),
                       CheckerOperator = CheckerOperator.And
                    }
                ]
            });
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, customRequestCheckers, options, logger);
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
            var options = Substitute.For<IOptions<CustomAwareAntiForgeryOptions>>();
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
            options.Value.Returns(new CustomAwareAntiForgeryOptions
            {
                CheckerGroups =
               [
                   new CheckerGroup
                    {
                       TypeNames = customRequestCheckers.Select(c => c.GetType().Name).ToArray(),
                       CheckerOperator = CheckerOperator.And
                    }
               ]
            });
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, customRequestCheckers, options, logger);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.DidNotReceive().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("All groups passed > skipping antiforgery.");
        }

        [Fact]
        public async Task OnAuthorizationAsync_Enforce_ForNonCustomUnsafeRequestWithAllAndCheckersFailsButCypressPasses()
        {
            // Arrange
            var antiforgery = Substitute.For<IAntiforgery>();
            var options = Substitute.For<IOptions<CustomAwareAntiForgeryOptions>>();
            antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
            var logger = Substitute.For<ILogger<CustomAwareAntiForgeryFilter>>();
            var customRequestChecker = Substitute.For<ICustomRequestChecker>(); 
            customRequestChecker.IsValidRequest(Arg.Any<HttpContext>()).Returns(false);
            var anotherRequestChecker = Substitute.For<ICustomRequestChecker>(); 
            anotherRequestChecker.IsValidRequest(Arg.Any<HttpContext>()).Returns(true);
            var cypressRequestChecker = Substitute.For<ICustomRequestChecker>(); 
            cypressRequestChecker.IsValidRequest(Arg.Any<HttpContext>()).Returns(true);
            var customRequestCheckers = new List<ICustomRequestChecker>
            {
                customRequestChecker,
                anotherRequestChecker,
                cypressRequestChecker
            };
            options.Value.Returns(new CustomAwareAntiForgeryOptions
            {
                CheckerGroups =
               [
                   new CheckerGroup
                    {
                       TypeNames = [customRequestChecker.GetType().Name, anotherRequestChecker.GetType().Name],
                       CheckerOperator = CheckerOperator.And
                    },
                     new CheckerGroup
                      {
                          TypeNames = [cypressRequestChecker.GetType().Name],
                          CheckerOperator = CheckerOperator.Or
                      }
               ]
            });
            var filter = new CustomAwareAntiForgeryFilter(antiforgery, customRequestCheckers, options, logger);
            var context = CreateAuthorizationFilterContext("POST");

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            await antiforgery.Received().ValidateRequestAsync(context.HttpContext);
            logger.Received().LogInformation("Enforcing anti-forgery for the request.");
        }
    }
}
