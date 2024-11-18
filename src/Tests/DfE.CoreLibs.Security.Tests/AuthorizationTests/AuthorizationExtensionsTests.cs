//using System.Collections.Generic;
//using System.Linq;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using DfE.CoreLibs.Security.Authorization;
//using DfE.CoreLibs.Security.Configurations;
//using DfE.CoreLibs.Security.Interfaces;
//using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Xunit;

//namespace DfE.CoreLibs.Security.Tests.AuthorizationTests
//{
//    /// <summary>
//    /// Custom authorization requirement for testing purposes.
//    /// </summary>
//    public class CustomRequirement : IAuthorizationRequirement { }

//    /// <summary>
//    /// Unit tests for the AddApplicationAuthorization extension method.
//    /// </summary>
//    public class AuthorizationExtensionsTests
//    {
//        private readonly IConfiguration _configuration;
//        private readonly ServiceProvider _serviceProvider;

//        /// <summary>
//        /// Initializes a new instance of the <see cref="AuthorizationExtensionsTests"/> class.
//        /// Sets up the configuration and service provider for testing.
//        /// </summary>
//        public AuthorizationExtensionsTests()
//        {
//            // Build configuration from the test project's appsettings.json
//            _configuration = new ConfigurationBuilder()
//                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//                .Build();

//            // Set up the service collection
//            var services = new ServiceCollection();

//            // Register logging services
//            services.AddLogging();

//            // Add application authorization
//            services.AddApplicationAuthorization(_configuration);

//            // Add Authorization services
//            services.AddAuthorization();

//            // Register a dummy IClaimsTransformation if necessary
//            services.AddTransient<IClaimsTransformation, DummyClaimsTransformation>();

//            // Register the AuthorizationPolicyProvider
//            services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();

//            // Register the IAuthorizationService
//            services.AddSingleton<IAuthorizationService, DefaultAuthorizationService>();

//            // Build the service provider
//            _serviceProvider = services.BuildServiceProvider();
//        }

//        /// <summary>
//        /// Tests that the authorization policies are loaded correctly from the configuration.
//        /// </summary>
//        [Fact]
//        public async Task AddApplicationAuthorization_ShouldLoadPoliciesFromConfiguration()
//        {
//            // Arrange
//            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();

//            // Define a test user with required scopes and roles
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim("http://schemas.microsoft.com/identity/claims/scope", "SCOPE.API.Read SCOPE.API.Write"),
//                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", "API.Read"),
//                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", "API.Write"),
//                new Claim("API.PersonalInfo", "true")
//            }, "TestAuthentication"));

//            // Act & Assert

//            // Test "CanRead" policy
//            var canReadResult = await authorizationService.AuthorizeAsync(user, null, "CanRead");
//            Assert.True(canReadResult.Succeeded, "User should be authorized for CanRead policy.");

//            // Test "CanReadWrite" policy
//            var canReadWriteResult = await authorizationService.AuthorizeAsync(user, null, "CanReadWrite");
//            Assert.True(canReadWriteResult.Succeeded, "User should be authorized for CanReadWrite policy.");

//            // Test "CanReadWritePlus" policy
//            var canReadWritePlusResult = await authorizationService.AuthorizeAsync(user, null, "CanReadWritePlus");
//            Assert.True(canReadWritePlusResult.Succeeded, "User should be authorized for CanReadWritePlus policy.");
//        }

//        /// <summary>
//        /// Tests that authorization succeeds when the user has all required roles, even if some scopes are missing.
//        /// </summary>
//        [Fact]
//        public async Task AddApplicationAuthorization_ShouldAuthorizeWhenUserHasAllRolesEvenIfMissingSomeScopes()
//        {
//            // Arrange
//            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();

//            // Define a test user with all required roles but missing one scope
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", "API.Read"),
//                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", "API.Write"),
//                new Claim("http://schemas.microsoft.com/identity/claims/scope", "SCOPE.API.Read")
//                // Missing "SCOPE.API.Write" scope
//            }, "TestAuthentication"));

//            // Act
//            var canReadWriteResult = await authorizationService.AuthorizeAsync(user, null, "CanReadWrite");

//            // Assert
//            Assert.True(canReadWriteResult.Succeeded, "User should be authorized for CanReadWrite policy because they have all required roles.");
//        }

//        /// <summary>
//        /// Tests that authorization fails when the user lacks both required roles and scopes.
//        /// </summary>
//        [Fact]
//        public async Task AddApplicationAuthorization_ShouldFailWhenUserLacksRequiredRolesAndScopes()
//        {
//            // Arrange
//            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();

//            // Define a test user missing all required roles and all required scopes
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                // No scope claims
//                // No role claims
//            }, "TestAuthentication"));

//            // Act
//            var canReadWriteResult = await authorizationService.AuthorizeAsync(user, null, "CanReadWrite");

//            // Assert
//            Assert.False(canReadWriteResult.Succeeded, "User should not be authorized for CanReadWrite policy when missing required roles and scopes.");
//        }

//        /// <summary>
//        /// Tests that authorization fails when the user lacks required roles but has all required scopes.
//        /// This test ensures that having scopes overrides missing roles.
//        /// </summary>
//        [Fact]
//        public async Task AddApplicationAuthorization_ShouldAuthorizeWhenUserHasAllScopesEvenIfMissingSomeRoles()
//        {
//            // Arrange
//            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();

//            // Define a test user with all required scopes but missing one role
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim("http://schemas.microsoft.com/identity/claims/scope", "SCOPE.API.Read SCOPE.API.Write"),
//                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", "API.Read")
//                // Missing "API.Write" role
//            }, "TestAuthentication"));

//            // Act
//            var canReadWriteResult = await authorizationService.AuthorizeAsync(user, null, "CanReadWrite");

//            // Assert
//            Assert.True(canReadWriteResult.Succeeded, "User should be authorized for CanReadWrite policy because they have all required scopes.");
//        }

//        /// <summary>
//        /// Tests that authorization succeeds when all required scopes are present, even if some roles are missing.
//        /// </summary>
//        [Fact]
//        public async Task AddApplicationAuthorization_ShouldAuthorizeCanReadWritePolicy_WhenAllScopesPresent()
//        {
//            // Arrange
//            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();

//            // Define a test user with all required scopes and some roles
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim("http://schemas.microsoft.com/identity/claims/scope", "SCOPE.API.Read SCOPE.API.Write"),
//                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", "API.Read")
//                // Missing "API.Write" role
//            }, "TestAuthentication"));

//            // Act
//            var canReadWriteResult = await authorizationService.AuthorizeAsync(user, null, "CanReadWrite");

//            // Assert
//            Assert.True(canReadWriteResult.Succeeded, "User should be authorized for CanReadWrite policy because they have all required scopes.");
//        }

//        /// <summary>
//        /// Tests that custom requirements can be added via policy customizations.
//        /// </summary>
//        [Fact]
//        public async Task AddApplicationAuthorization_ShouldAddCustomRequirementViaAction()
//        {
//            // Arrange
//            var services = new ServiceCollection();

//            // Register logging services
//            services.AddLogging();

//            // Define a custom requirement
//            var customRequirement = new CustomRequirement();

//            // Build in-memory policy configurations including a custom policy
//            var policyDefinitions = new List<PolicyDefinition>
//            {
//                new PolicyDefinition
//                {
//                    Name = "CustomPolicy",
//                    Operator = "OR",
//                    Roles = new List<string> { "API.CustomRole" },
//                    Scopes = new List<string> { "SCOPE.API.CustomScope" }
//                }
//            };

//            // Manually add the policies to the appsettings.json-like configuration
//            var inMemorySettings = new Dictionary<string, string>();
//            int index = 0;
//            foreach (var policy in policyDefinitions)
//            {
//                inMemorySettings.Add($"Authorization:Policies:{index}:Name", policy.Name);
//                inMemorySettings.Add($"Authorization:Policies:{index}:Operator", policy.Operator);

//                if (policy.Roles != null)
//                {
//                    for (int i = 0; i < policy.Roles.Count; i++)
//                    {
//                        inMemorySettings.Add($"Authorization:Policies:{index}:Roles:{i}", policy.Roles[i]);
//                    }
//                }

//                if (policy.Scopes != null)
//                {
//                    for (int i = 0; i < policy.Scopes.Count; i++)
//                    {
//                        inMemorySettings.Add($"Authorization:Policies:{index}:Scopes:{i}", policy.Scopes[i]);
//                    }
//                }

//                index++;
//            }

//            var testConfiguration = new ConfigurationBuilder()
//                .AddInMemoryCollection(inMemorySettings)
//                .Build();

//            // Add application authorization with custom policy customization
//            services.AddApplicationAuthorization(testConfiguration, new Dictionary<string, Action<AuthorizationPolicyBuilder>>
//            {
//                { "CustomPolicy", builder => builder.Requirements.Add(customRequirement) }
//            });

//            // Add Authorization services
//            services.AddAuthorization();

//            // Register a dummy IClaimsTransformation if necessary
//            services.AddTransient<IClaimsTransformation, DummyClaimsTransformation>();

//            // Register the AuthorizationPolicyProvider
//            services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();

//            // Register the IAuthorizationService
//            services.AddSingleton<IAuthorizationService, DefaultAuthorizationService>();

//            // Build the service provider
//            var provider = services.BuildServiceProvider();
//            var authorizationService = provider.GetRequiredService<IAuthorizationService>();

//            // Define a test user that meets the custom policy requirements
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim("http://schemas.microsoft.com/identity/claims/scope", "SCOPE.API.CustomScope"),
//                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", "API.CustomRole")
//            }, "TestAuthentication"));

//            // Act
//            var customPolicyResult = await authorizationService.AuthorizeAsync(user, null, "CustomPolicy");

//            // Assert
//            Assert.True(customPolicyResult.Succeeded, "User should be authorized for CustomPolicy with the custom requirement.");
//        }

//        /// <summary>
//        /// Tests that custom requirements can be added to existing policies via policy customizations.
//        /// </summary>
//        [Fact]
//        public async Task AddApplicationAuthorization_ShouldAddCustomRequirementToExistingPolicy()
//        {
//            // Arrange
//            var services = new ServiceCollection();

//            // Register logging services
//            services.AddLogging();

//            // Define a custom requirement
//            var customRequirement = new CustomRequirement();

//            // Build in-memory policy configurations including an existing policy
//            var policyDefinitions = new List<PolicyDefinition>
//            {
//                new PolicyDefinition
//                {
//                    Name = "CanReadWritePlus",
//                    Operator = "AND",
//                    Roles = new List<string> { "API.Read", "API.Write" },
//                    Scopes = new List<string> { "SCOPE.API.Read", "SCOPE.API.Write" },
//                    Claims = new List<ClaimDefinition>
//                    {
//                        new ClaimDefinition
//                        {
//                            Type = "API.PersonalInfo",
//                            Values = new List<string> { "true" }
//                        }
//                    }
//                }
//            };

//            // Manually add the policies to the appsettings.json-like configuration
//            var inMemorySettings = new Dictionary<string, string>();
//            int index = 0;
//            foreach (var policy in policyDefinitions)
//            {
//                inMemorySettings.Add($"Authorization:Policies:{index}:Name", policy.Name);
//                inMemorySettings.Add($"Authorization:Policies:{index}:Operator", policy.Operator);

//                if (policy.Roles != null)
//                {
//                    for (int i = 0; i < policy.Roles.Count; i++)
//                    {
//                        inMemorySettings.Add($"Authorization:Policies:{index}:Roles:{i}", policy.Roles[i]);
//                    }
//                }

//                if (policy.Scopes != null)
//                {
//                    for (int i = 0; i < policy.Scopes.Count; i++)
//                    {
//                        inMemorySettings.Add($"Authorization:Policies:{index}:Scopes:{i}", policy.Scopes[i]);
//                    }
//                }

//                if (policy.Claims != null)
//                {
//                    for (int i = 0; i < policy.Claims.Count; i++)
//                    {
//                        var claim = policy.Claims[i];
//                        inMemorySettings.Add($"Authorization:Policies:{index}:Claims:{i}:Type", claim.Type);
//                        if (claim.Values != null)
//                        {
//                            for (int j = 0; j < claim.Values.Count; j++)
//                            {
//                                inMemorySettings.Add($"Authorization:Policies:{index}:Claims:{i}:Values:{j}", claim.Values[j]);
//                            }
//                        }
//                    }
//                }

//                index++;
//            }

//            var testConfiguration = new ConfigurationBuilder()
//                .AddInMemoryCollection(inMemorySettings)
//                .Build();

//            // Add application authorization with custom policy customization
//            services.AddApplicationAuthorization(testConfiguration, new Dictionary<string, Action<AuthorizationPolicyBuilder>>
//            {
//                { "CanReadWritePlus", builder => builder.Requirements.Add(customRequirement) }
//            });

//            // Add Authorization services
//            services.AddAuthorization();

//            // Register a dummy IClaimsTransformation if necessary
//            services.AddTransient<IClaimsTransformation, DummyClaimsTransformation>();

//            // Register the AuthorizationPolicyProvider
//            services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();

//            // Register the IAuthorizationService
//            services.AddSingleton<IAuthorizationService, DefaultAuthorizationService>();

//            // Build the service provider
//            var provider = services.BuildServiceProvider();
//            var authorizationService = provider.GetRequiredService<IAuthorizationService>();

//            // Define a test user that meets the existing policy and the custom requirement
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim("http://schemas.microsoft.com/identity/claims/scope", "SCOPE.API.Read SCOPE.API.Write"),
//                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", "API.Read"),
//                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", "API.Write"),
//                new Claim("API.PersonalInfo", "true")
//            }, "TestAuthentication"));

//            // Act
//            var canReadWritePlusResult = await authorizationService.AuthorizeAsync(user, null, "CanReadWritePlus");

//            // Assert
//            Assert.True(canReadWritePlusResult.Succeeded, "User should be authorized for CanReadWritePlus policy with the custom requirement.");
//        }
//    }

//    /// <summary>
//    /// A dummy implementation of IClaimsTransformation for testing purposes.
//    /// </summary>
//    public class DummyClaimsTransformation : IClaimsTransformation
//    {
//        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
//        {
//            // No transformation; return the principal as is
//            return Task.FromResult(principal);
//        }
//    }
//}
