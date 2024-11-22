using System.Diagnostics.CodeAnalysis;
using DfE.CoreLibs.Security.Authorization;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace DfE.CoreLibs.Security.Tests.AuthorizationTests
{
    public class AuthorizationExtensionsTests
    {
        private readonly IConfiguration _configuration;
        private readonly ServiceProvider _serviceProvider;

        public AuthorizationExtensionsTests()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();

            services.AddLogging();

            services.AddApplicationAuthorization(_configuration);

            services.AddAuthorization();

            services.AddTransient<IClaimsTransformation, DummyClaimsTransformation>();

            services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();

            services.AddSingleton<IAuthorizationService, DefaultAuthorizationService>();

            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Tests that the authorization policies are loaded correctly from the configuration.
        /// </summary>
        [Fact]
        public async Task AddApplicationAuthorization_ShouldLoadPoliciesFromConfiguration()
        {
            // Arrange
            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();

            var userWithScopes = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/scope", "SCOPE.API.Read SCOPE.API.Write")
            }, "TestAuthentication"));

            var userWithRoles = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, "API.Read"),
                new Claim(ClaimTypes.Role, "API.Write")
            }, "TestAuthentication"));

            // Act & Assert
            var canReadResultScopes = await authorizationService.AuthorizeAsync(userWithScopes, null, "CanRead");
            Assert.True(canReadResultScopes.Succeeded, "User with scopes should be authorized for CanRead policy.");

            var canReadResultRoles = await authorizationService.AuthorizeAsync(userWithRoles, null, "CanRead");
            Assert.True(canReadResultRoles.Succeeded, "User with roles should be authorized for CanRead policy.");

            var canReadWriteResultScopes =
                await authorizationService.AuthorizeAsync(userWithScopes, null, "CanReadWrite");
            Assert.True(canReadWriteResultScopes.Succeeded,
                "User with scopes should be authorized for CanReadWrite policy.");

            var canReadWriteResultRoles =
                await authorizationService.AuthorizeAsync(userWithRoles, null, "CanReadWrite");
            Assert.True(canReadWriteResultRoles.Succeeded,
                "User with roles should be authorized for CanReadWrite policy.");

            var userWithScopesAndClaims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/scope", "SCOPE.API.Read SCOPE.API.Write"),
                new Claim("API.PersonalInfo", "true")
            }, "TestAuthentication"));

            var canReadWritePlusResultScopes =
                await authorizationService.AuthorizeAsync(userWithScopesAndClaims, null, "CanReadWritePlus");
            Assert.True(canReadWritePlusResultScopes.Succeeded,
                "User with scopes and required claims should be authorized for CanReadWritePlus policy.");

            var userWithRolesAndClaims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, "API.Read"),
                new Claim(ClaimTypes.Role, "API.Write"),
                new Claim("API.PersonalInfo", "true")
            }, "TestAuthentication"));

            var canReadWritePlusResultRoles =
                await authorizationService.AuthorizeAsync(userWithRolesAndClaims, null, "CanReadWritePlus");
            Assert.True(canReadWritePlusResultRoles.Succeeded,
                "User with roles and required claims should be authorized for CanReadWritePlus policy.");
        }

        [Fact]
        public async Task AddApplicationAuthorization_ShouldAuthorizeWhenUserHasAllScopesEvenIfMissingSomeRoles()
        {
            // Arrange
            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/scope", "SCOPE.API.Read SCOPE.API.Write")
                // No role claims
            }, "TestAuthentication"));

            // Act
            var canReadWriteResult = await authorizationService.AuthorizeAsync(user, null, "CanReadWrite");

            // Assert
            Assert.True(canReadWriteResult.Succeeded,
                "User should be authorized for CanReadWrite policy because they have all required scopes.");
        }

        [Fact]
        public async Task AddApplicationAuthorization_ShouldAuthorizeWhenUserHasAllRolesEvenIfMissingSomeScopes()
        {
            // Arrange
            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, "API.Read"),
                new Claim(ClaimTypes.Role, "API.Write")
                // No scope claims
            }, "TestAuthentication"));

            // Act
            var canReadWriteResult = await authorizationService.AuthorizeAsync(user, null, "CanReadWrite");

            // Assert
            Assert.True(canReadWriteResult.Succeeded,
                "User should be authorized for CanReadWrite policy because they have all required roles.");
        }

        [Fact]
        public async Task AddApplicationAuthorization_ShouldFailWhenUserLacksRequiredScopesAndRoles()
        {
            // Arrange
            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                // No scope claims
                // No role claims
            }, "TestAuthentication"));

            // Act
            var canReadWriteResult = await authorizationService.AuthorizeAsync(user, null, "CanReadWrite");

            // Assert
            Assert.False(canReadWriteResult.Succeeded,
                "User should not be authorized for CanReadWrite policy when missing required scopes and roles.");
        }

        [Fact]
        public async Task AddApplicationAuthorization_ShouldFailWhenUserLacksRequiredScopes()
        {
            // Arrange
            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/scope",
                    "SCOPE.API.Read")
                // No role claims
            }, "TestAuthentication"));

            // Act
            var canReadWriteResult = await authorizationService.AuthorizeAsync(user, null, "CanReadWrite");

            // Assert
            Assert.False(canReadWriteResult.Succeeded,
                "User should not be authorized for CanReadWrite policy when missing required scopes.");
        }

        [Fact]
        public async Task AddApplicationAuthorization_ShouldFailWhenUserLacksRequiredRoles()
        {
            // Arrange
            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();

            var user = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.Role, "API.Read") // Missing "API.Write"
                // No scope claims
            ], "TestAuthentication"));

            // Act
            var canReadWriteResult = await authorizationService.AuthorizeAsync(user, null, "CanReadWrite");

            // Assert
            Assert.False(canReadWriteResult.Succeeded,
                "User should not be authorized for CanReadWrite policy when missing required roles.");
        }

        [Fact]
        public void AddCustomClaimProvider_ShouldRegisterClaimProvider()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCustomClaimProvider<DummyCustomClaimProvider>();
            var provider = services.BuildServiceProvider();
            var registeredProvider = provider.GetService<ICustomClaimProvider>();

            // Assert
            Assert.NotNull(registeredProvider);
            Assert.IsType<DummyCustomClaimProvider>(registeredProvider);
        }

        [Fact]
        public async Task AddApplicationAuthorization_ShouldApplyPolicyCustomizations()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Authorization:Policies:0:Name", "CustomPolicy" },
                }!)
                .Build();

            var customizations = new Dictionary<string, Action<AuthorizationPolicyBuilder>>
            {
                {
                    "CustomPolicy", builder =>
                    {
                        builder.RequireClaim("CustomClaim", "CustomValue");
                    }
                }
            };

            // Act
            services.AddApplicationAuthorization(configuration, customizations);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IAuthorizationPolicyProvider>();

            // Assert
            var policy = await options.GetPolicyAsync("CustomPolicy");
            Assert.NotNull(policy);
            Assert.Contains(policy.Requirements, r => r is ClaimsAuthorizationRequirement claimReq &&
                                                      claimReq.ClaimType == "CustomClaim" &&
                                                      claimReq.AllowedValues!.Contains("CustomValue"));
        }

        [Fact]
        public async Task AddApplicationAuthorization_ShouldAuthorizeWhenPolicyOperatorIsAnd()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Authorization:Policies:0:Name", "AndPolicy" },
                    { "Authorization:Policies:0:Operator", "AND" },
                    { "Authorization:Policies:0:Scopes:0", "Scope1" },
                }!)
                .Build();

            services.AddApplicationAuthorization(configuration);
            var provider = services.BuildServiceProvider();
            var authorizationService = provider.GetRequiredService<IAuthorizationService>();

            // User with both scope and role
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/scope", "Scope1"),
            }, "TestAuthentication"));

            // Act
            var result = await authorizationService.AuthorizeAsync(user, null, "AndPolicy");

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task AddApplicationAuthorization_ShouldAuthorizeWhenPolicyOperatorIsOr()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Authorization:Policies:0:Name", "OrPolicy" },
                    { "Authorization:Policies:0:Operator", "OR" },
                    { "Authorization:Policies:0:Roles:0", "Role1" },
                    { "Authorization:Policies:0:Roles:1", "Role2" }

                }!)
                .Build();

            services.AddApplicationAuthorization(configuration);
            var provider = services.BuildServiceProvider();
            var authorizationService = provider.GetRequiredService<IAuthorizationService>();

            // User with only scope
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "Role2")
            }, "TestAuthentication"));

            // Act
            var result = await authorizationService.AuthorizeAsync(user, null, "OrPolicy");

            // Assert
            Assert.True(result.Succeeded);
        }


        [Fact]
        public async Task AddApplicationAuthorization_ShouldWorkWithoutPolicyCustomizations()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Authorization:Policies:0:Name", "PolicyWithoutCustomizations" }
                }!)
                .Build();

            // Act
            services.AddApplicationAuthorization(configuration, policyCustomizations: null);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IAuthorizationPolicyProvider>();

            // Assert
            var policy = await options.GetPolicyAsync("PolicyWithoutCustomizations");
            Assert.NotNull(policy);
        }

        [Fact]
        public void AddApplicationAuthorization_ShouldHandleNullPolicies()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()!)
                .Build(); // No policies in config

            // Act
            services.AddApplicationAuthorization(configuration);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IAuthorizationPolicyProvider>();

            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await options.GetPolicyAsync("NonExistentPolicy"));
        }

    }

    /// <summary>
    /// A dummy implementation of IClaimsTransformation for testing purposes.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DummyClaimsTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // No transformation; return the principal as is
            return Task.FromResult(principal);
        }
    }

    /// <summary>
    /// Dummy implementation of ICustomClaimProvider for testing purposes.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DummyCustomClaimProvider : ICustomClaimProvider
    {
        public Task<IEnumerable<Claim>> GetClaimsAsync(ClaimsPrincipal principal)
        {
            return Task.FromResult<IEnumerable<Claim>>(new[] { new Claim("DummyClaim", "DummyValue") });
        }
    }
}
