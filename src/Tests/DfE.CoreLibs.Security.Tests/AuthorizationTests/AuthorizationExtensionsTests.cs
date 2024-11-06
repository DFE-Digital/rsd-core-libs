using DfE.CoreLibs.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DfE.CoreLibs.Security.Tests.AuthorizationTests
{
    public class AuthorizationExtensionsTests
    {
        private readonly IConfiguration _configuration;

        // Constructor to initialize shared configuration
        public AuthorizationExtensionsTests()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        [Fact]
        public void AddApplicationAuthorization_ShouldLoadPoliciesFromConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApplicationAuthorization(_configuration);

            var provider = services.BuildServiceProvider();
            var authorizationOptions = provider.GetRequiredService<IAuthorizationPolicyProvider>() as DefaultAuthorizationPolicyProvider;

            // Assert that policies are loaded correctly
            Assert.NotNull(authorizationOptions);

            // Check each policy
            var canReadPolicy = authorizationOptions!.GetPolicyAsync("CanRead").Result;
            Assert.NotNull(canReadPolicy);
            Assert.Contains(canReadPolicy.Requirements, r => r is RolesAuthorizationRequirement);

            var canReadWritePolicy = authorizationOptions.GetPolicyAsync("CanReadWrite").Result;
            Assert.NotNull(canReadWritePolicy);
            Assert.Contains(canReadWritePolicy.Requirements, r => r is RolesAuthorizationRequirement);

            var canReadWritePlusPolicy = authorizationOptions.GetPolicyAsync("CanReadWritePlus").Result;
            Assert.NotNull(canReadWritePlusPolicy);
            Assert.Contains(canReadWritePlusPolicy.Requirements, r => r is RolesAuthorizationRequirement);
            Assert.Contains(canReadWritePlusPolicy.Requirements, r => r is ClaimsAuthorizationRequirement);
        }

        [Fact]
        public void AddApplicationAuthorization_ShouldApplyOrLogicForRoles()
        {
            // Arrange
            var services = new ServiceCollection();

            services.AddApplicationAuthorization(_configuration);

            var provider = services.BuildServiceProvider();
            var authorizationOptions = provider.GetRequiredService<IAuthorizationPolicyProvider>() as DefaultAuthorizationPolicyProvider;

            // Assert
            Assert.NotNull(authorizationOptions);
            var policy = authorizationOptions!.GetPolicyAsync("TestPolicy").Result;
            Assert.NotNull(policy);
            var roleRequirement = policy.Requirements.OfType<RolesAuthorizationRequirement>().SingleOrDefault();
            Assert.NotNull(roleRequirement);
            Assert.Contains("Role1", roleRequirement!.AllowedRoles);
            Assert.Contains("Role2", roleRequirement.AllowedRoles);
        }

        [Fact]
        public void AddApplicationAuthorization_ShouldApplyAndLogicForRoles()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApplicationAuthorization(_configuration);

            var provider = services.BuildServiceProvider();
            var authorizationOptions = provider.GetRequiredService<IAuthorizationPolicyProvider>() as DefaultAuthorizationPolicyProvider;

            // Assert
            Assert.NotNull(authorizationOptions);
            var policy = authorizationOptions!.GetPolicyAsync("TestPolicyAND").Result;
            Assert.NotNull(policy);
            var roleRequirements = policy.Requirements.OfType<RolesAuthorizationRequirement>().ToList();
            Assert.Equal(2, roleRequirements.Count);
            Assert.Contains(roleRequirements, r => r.AllowedRoles.Contains("Role1"));
            Assert.Contains(roleRequirements, r => r.AllowedRoles.Contains("Role2"));
        }

        [Fact]
        public void AddApplicationAuthorization_ShouldAddCustomRequirementViaAction()
        {
            // Arrange
            var services = new ServiceCollection();

            var requirement = new CustomRequirement();
            var policyCustomizations = new Dictionary<string, Action<AuthorizationPolicyBuilder>>
            {
                {
                    "CustomPolicy", builder => builder.Requirements.Add(requirement)
                }
            };

            // Act
            services.AddApplicationAuthorization(_configuration, policyCustomizations);

            var provider = services.BuildServiceProvider();
            var authorizationOptions = provider.GetRequiredService<IAuthorizationPolicyProvider>() as DefaultAuthorizationPolicyProvider;

            // Assert
            Assert.NotNull(authorizationOptions);
            var policy = authorizationOptions!.GetPolicyAsync("CustomPolicy").Result;
            Assert.NotNull(policy);
            Assert.Contains(policy.Requirements, r => r == requirement);
        }

        [Fact]
        public void AddApplicationAuthorization_ShouldAddCustomRequirementToExistingPolicy()
        {
            // Arrange
            var services = new ServiceCollection();
            var customRequirement = new CustomRequirement();

            var policyCustomizations = new Dictionary<string, Action<AuthorizationPolicyBuilder>>
            {
                {
                    "CanReadWritePlus", builder => builder.Requirements.Add(customRequirement)
                }
            };

            // Act
            services.AddApplicationAuthorization(_configuration, policyCustomizations);

            var provider = services.BuildServiceProvider();
            var authorizationOptions = provider.GetRequiredService<IAuthorizationPolicyProvider>() as DefaultAuthorizationPolicyProvider;

            // Assert
            Assert.NotNull(authorizationOptions);

            // Retrieve the existing policy
            var policy = authorizationOptions!.GetPolicyAsync("CanReadWritePlus").Result;
            Assert.NotNull(policy);

            // Verify that the original roles and claims are present
            var roleRequirements = policy.Requirements.OfType<RolesAuthorizationRequirement>();
            Assert.NotNull(roleRequirements);
            Assert.Contains(roleRequirements, r => r.AllowedRoles.Contains("API.Read"));
            Assert.Contains(roleRequirements, r => r.AllowedRoles.Contains("API.Write"));

            var claimsRequirements = policy.Requirements.OfType<ClaimsAuthorizationRequirement>();
            Assert.NotNull(claimsRequirements);
            Assert.Contains(claimsRequirements, cr => cr.ClaimType == "API.PersonalInfo");

            Assert.Contains(claimsRequirements, cr => cr.ClaimType == "API.PersonalInfo" && cr.AllowedValues.Contains("true"));

            Assert.Contains(policy.Requirements, r => r == customRequirement);
        }
    }

    public class CustomRequirement : IAuthorizationRequirement { }
}
