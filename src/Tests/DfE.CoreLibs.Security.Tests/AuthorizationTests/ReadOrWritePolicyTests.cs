using DfE.CoreLibs.Security.Authorization;
using DfE.CoreLibs.Security.Extensions;
using global::DfE.CoreLibs.Security.Authorization.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DfE.CoreLibs.Security.Tests.AuthorizationTests
{
    public class ReadOrWritePolicyTests
    {
        private readonly IAuthorizationService _authService;

        public ReadOrWritePolicyTests()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            // Generate Read/Write policies automatically
            services.AddLogging();
            services.AddApplicationAuthorization(
                config,
                policyCustomizations: new Dictionary<string, Action<AuthorizationPolicyBuilder>>
                {
                    ["ReadOrWrite"] = pb => pb.RequireAssertion(ctx =>
                    {
                        var res = ctx.Resource as string ?? "";
                        return ctx.User.HasPermission(res, "Read")
                            || ctx.User.HasPermission(res, "Write");
                    })
                },
                configureResourcePolicies: opts =>
                {
                    opts.Actions.AddRange(["Read", "Write"]);
                });

            services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();
            services.AddSingleton<IAuthorizationHandler, ResourcePermissionHandler>();
            services.AddSingleton<IAuthorizationService, DefaultAuthorizationService>();

            _authService = services.BuildServiceProvider()
                             .GetRequiredService<IAuthorizationService>();
        }

        [Fact]
        public async Task ReadOrWrite_Succeeds_OnReadOnly()
        {
            var user = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("permission", "foo:Read")
                }));

            var result = await _authService.AuthorizeAsync(user, "foo", "ReadOrWrite");
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task ReadOrWrite_Succeeds_OnWriteOnly()
        {
            var user = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("permission", "foo:Write")
                }));

            var result = await _authService.AuthorizeAsync(user, "foo", "ReadOrWrite");
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task ReadOrWrite_Fails_WhenNeither()
        {
            var user = new System.Security.Claims.ClaimsPrincipal();
            var result = await _authService.AuthorizeAsync(user, "foo", "ReadOrWrite");
            Assert.False(result.Succeeded);
        }
    }
}
