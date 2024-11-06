using DfE.CoreLibs.Security.Configurations;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        /// <param name="policyCustomizations">The the customizations such as Requirements which can be added to the policy after it is created.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddApplicationAuthorization(
            this IServiceCollection services,
            IConfiguration configuration,
            Dictionary<string, Action<AuthorizationPolicyBuilder>>? policyCustomizations = null)
        {
            services.AddAuthorization(options =>
            {
                var policies = configuration.GetSection("Authorization:Policies").Get<List<PolicyDefinition>>();

                foreach (var policyConfig in policies ?? [])
                {
                    options.AddPolicy(policyConfig.Name, policyBuilder =>
                    {
                        policyBuilder.RequireAuthenticatedUser();

                        if (policyConfig.Roles.Any())
                        {
                            if (string.Equals(policyConfig.Operator, "AND", StringComparison.OrdinalIgnoreCase))
                            {
                                // Use AND logic: require each role individually
                                foreach (var role in policyConfig.Roles)
                                {
                                    policyBuilder.RequireRole(role);
                                }
                            }
                            else
                            {
                                // Use OR logic: require any of the roles
                                policyBuilder.RequireRole(policyConfig.Roles.ToArray());
                            }
                        }

                        if (policyConfig.Claims != null && policyConfig.Claims.Any())
                        {
                            foreach (var claim in policyConfig.Claims)
                            {
                                policyBuilder.RequireClaim(claim.Type, claim.Values.ToArray());
                            }
                        }
                    });
                }

                if (policyCustomizations != null)
                {
                    foreach (var (policyName, customization) in policyCustomizations)
                    {
                        if (options.GetPolicy(policyName) is not null)
                        {
                            // If the policy already exists, modify it
                            UpdateExistingPolicy(options, policyName, customization);
                        }
                        else
                        {
                            // If the policy does not exist, create a new one
                            options.AddPolicy(policyName, customization);
                        }
                    }
                }
            });

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

    }
}
