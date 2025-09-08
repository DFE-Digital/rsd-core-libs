using GovUK.Dfe.CoreLibs.Security.TagHelpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace GovUK.Dfe.CoreLibs.Security.Tests.AuthorizationTests
{
    public class PolicyAuthorizeTagHelperTests
    {
        private class StubAuthService(bool succeed) : IAuthorizationService
        {
            public Task<AuthorizationResult> AuthorizeAsync(
                ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
                => throw new System.NotImplementedException();

            public Task<AuthorizationResult> AuthorizeAsync(
                ClaimsPrincipal user, object? resource, string policyName)
                => Task.FromResult(succeed
                    ? AuthorizationResult.Success()
                    : AuthorizationResult.Failed());

            public Task<AuthorizationResult> AuthorizeAsync(
                ClaimsPrincipal user, object? resource, string policyName, params IAuthorizationRequirement[] requirements)
                => AuthorizeAsync(user, resource, policyName);
        }

        private static TagHelperContext MakeContext() =>
            new(
                tagName: "authorize",
                allAttributes: new TagHelperAttributeList
                {
                    { "resource", "res1" },
                    { "policy", "P" }
                },
                items: new Dictionary<object, object>(),
                uniqueId: "test");

        private static TagHelperOutput MakeOutput(string initialContent) =>
            new TagHelperOutput(
                "authorize",
                [],
                (useCached, encoder) =>
                    Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetContent(initialContent)));

        [Fact]
        public async Task ProcessAsync_RendersContent_WhenAuthorized()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IAuthorizationService>(new StubAuthService(true));
            var provider = services.BuildServiceProvider();

            var ctx = new DefaultHttpContext
            {
                RequestServices = provider,
                User = new ClaimsPrincipal(new ClaimsIdentity("TestAuth"))
            };

            var tag = new PolicyAuthorizeTagHelper(
                provider.GetRequiredService<IAuthorizationService>(),
                new HttpContextAccessor { HttpContext = ctx });

            var output = MakeOutput("hello");

            await tag.ProcessAsync(MakeContext(), output);

            var content = await output.GetChildContentAsync();

            Assert.Equal("authorize", output.TagName);
            Assert.Equal("hello", content.GetContent());
        }

        [Fact]
        public async Task ProcessAsync_SuppressesContent_WhenNotAuthorized()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IAuthorizationService>(new StubAuthService(false));
            var ctx = new DefaultHttpContext
            {
                RequestServices = services.BuildServiceProvider()
            };

            var tag = new PolicyAuthorizeTagHelper(
                ctx.RequestServices.GetRequiredService<IAuthorizationService>(),
                new HttpContextAccessor { HttpContext = ctx });

            var output = MakeOutput("world");
            await tag.ProcessAsync(MakeContext(), output);

            Assert.Null(output.TagName);
            Assert.Equal(string.Empty, output.Content.GetContent());
        }
    }
}
