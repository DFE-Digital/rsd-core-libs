using DfE.CoreLibs.Security.Configurations;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using DfE.CoreLibs.Security.Authorization.Requirements;
using DfE.CoreLibs.Security.Services;

namespace DfE.CoreLibs.Security.Authorization
{
    /// <summary>
    /// Extension methods for configuring application authorization and custom claim providers.
    /// </summary>
    public static class AuthorizationExtensions
    {
        /// <summary>
        /// Configures authorization policies based on roles, claims, and custom requirements.
        /// Loads policies from the provided configuration.
        /// </summary>
        /// <param name="services">The service collection to add authorization services to.</param>
        /// <param name="configuration">The configuration object containing policy definitions.</param>
        /// <param name="apiAuthenticationScheme">The authentication scheme.</param>
        /// <param name="policyCustomizations">The customizations such as Requirements which can be added to the policy after it is created.</param>
        /// <param name="configureResourcePolicies">The customisations such as resource policies can be added to a policy or form a new policy.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddApplicationAuthorization(
            this IServiceCollection services,
            IConfiguration configuration,
            Dictionary<string, Action<AuthorizationPolicyBuilder>>? policyCustomizations = null,
            string? apiAuthenticationScheme = null,
            Action<ResourcePermissionOptions>? configureResourcePolicies = null)
        {
            var resourceOpts = new ResourcePermissionOptions();
            configureResourcePolicies?.Invoke(resourceOpts);
            services.AddSingleton(resourceOpts);

            services.AddAuthorization(options =>
            {
                // Load policies from configuration
                var policies = configuration.GetSection("Authorization:Policies").Get<List<PolicyDefinition>>();

                foreach (var policyConfig in policies ?? [])
                {
                    options.AddPolicy(policyConfig.Name, policyBuilder =>
                    {
                        policyBuilder.RequireAuthenticatedUser();

                        // Specify the authentication scheme for the API
                        if (apiAuthenticationScheme != null)
                            policyBuilder.AuthenticationSchemes.Add(apiAuthenticationScheme);

                        if (string.Equals(policyConfig.Operator, "AND", StringComparison.OrdinalIgnoreCase))
                        {
                            // "AND" logic: User must have either all roles or all scopes
                            policyBuilder.RequireAssertion(context =>
                            {
                                var user = context.User;
                                var userScopes = GetUserScopes(user).ToArray();
#pragma warning disable S6603
                                if (userScopes.Any())
                                {
                                    // User has scopes, check if they have all required scopes
                                    var requiredScopes = policyConfig.Scopes ?? new List<string>();

                                    // Require all scopes (AND logic)
                                    return requiredScopes.All(scope => userScopes.Contains(scope));
                                }
                                else
                                {
                                    // User does not have scopes, check if they have all required roles
                                    var requiredRoles = policyConfig.Roles ?? [];

                                    // Require all roles (AND logic)
                                    return requiredRoles.All(role => user.IsInRole(role));
                                }
#pragma warning restore S6603
                            });
                        }
                        else // "OR" logic
                        {
                            // "OR" logic: user needs at least one role or one scope
                            policyBuilder.RequireAssertion(context =>
                            {
                                var user = context.User;
                                var userScopes = GetUserScopes(user).ToArray();

                                var hasAnyScope = false;
                                var hasAnyRole = false;

                                if (userScopes.Any())
                                {
                                    // User has scopes, check for any matching scope
                                    hasAnyScope = (policyConfig.Scopes ?? new List<string>())
                                        .Exists(scope => userScopes.Contains(scope));
                                }

                                // Check for any matching role
                                hasAnyRole = (policyConfig.Roles ?? [])
                                    .Exists(role => user.IsInRole(role));

                                // Succeed if the user has any matching role or scope
                                return hasAnyRole || hasAnyScope;
                            });
                        }

                        // Add any required claims for this policy
                        if (policyConfig.Claims != null && policyConfig.Claims.Any())
                        {
                            foreach (var claim in policyConfig.Claims)
                            {
                                policyBuilder.RequireClaim(claim.Type, claim.Values.ToArray());
                            }
                        }
                    });
                }

                // Apply customizations if provided
                if (policyCustomizations != null)
                {
                    foreach (var (policyName, customization) in policyCustomizations)
                    {
                        if (options.GetPolicy(policyName) is not null)
                        {
                            UpdateExistingPolicy(options, policyName, customization);
                        }
                        else
                        {
                            options.AddPolicy(policyName, customization);
                        }
                    }
                }

                // Auto-generate resource-based policies only if actions passed into the ResourcePermissionOptions
                if (resourceOpts.Actions?.Any() == true)
                {
                    foreach (var action in resourceOpts.Actions)
                    {
                        var policyName = string.Format(
                            resourceOpts.PolicyNameFormat, action);

                        // If the policy already exists in config, just append
                        if (options.GetPolicy(policyName) is not null)
                        {
                            UpdateExistingPolicy(options, policyName, pb =>
                                pb.Requirements.Add(
                                    new ResourcePermissionRequirement(
                                        action, resourceOpts.ClaimType)));
                        }
                        else
                        {
                            options.AddPolicy(policyName, pb =>
                                                    {
                                                        pb.RequireAuthenticatedUser();
                                                        if (apiAuthenticationScheme != null)
                                                            pb.AuthenticationSchemes.Add(apiAuthenticationScheme);
                                                        pb.Requirements.Add(
                                                            new ResourcePermissionRequirement(
                                                                action, resourceOpts.ClaimType));
                                                    });
                        }
                    }
                }

            });

            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddTransient<IClaimsTransformation, CustomClaimsTransformation>();

            return services;
        }


        /// <summary>
        /// Registers a custom claim provider to retrieve claims dynamically.
        /// </summary>
        /// <typeparam name="TProvider">The custom claim provider implementing ICustomClaimProvider.</typeparam>
        /// <param name="services">The service collection to add the claim provider to.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddCustomClaimProvider<TProvider>(this IServiceCollection services)
            where TProvider : class, ICustomClaimProvider
        {
            services.AddTransient<ICustomClaimProvider, TProvider>();
            return services;
        }

        /// <summary>
        /// Updates an existing policy with additional requirements from a customization action.
        /// </summary>
        private static void UpdateExistingPolicy(AuthorizationOptions options, string policyName, Action<AuthorizationPolicyBuilder> customization)
        {
            var existingPolicyBuilder = new AuthorizationPolicyBuilder();

            // Copy existing policy requirements
            var existingPolicy = options.GetPolicy(policyName)!;
            foreach (var requirement in existingPolicy.Requirements)
            {
                existingPolicyBuilder.Requirements.Add(requirement);
            }

            // Apply the new customization
            customization(existingPolicyBuilder);

            // Replace the policy with the updated one
            options.AddPolicy(policyName, existingPolicyBuilder.Build());
        }


        private static IEnumerable<string> GetUserScopes(ClaimsPrincipal user)
        {
            var scopes = new List<string>();

            // Define all possible claim types for scopes
            var scopeClaimTypes = new[]
            {
                "scp",
                "scope",
                "http://schemas.microsoft.com/identity/claims/scope"
            };

            foreach (var claimType in scopeClaimTypes)
            {
                var scopeClaims = user.FindAll(claimType);
                foreach (var claim in scopeClaims)
                {
                    scopes.AddRange(claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                }
            }

            return scopes.Distinct(StringComparer.OrdinalIgnoreCase);
        }

    }
}
