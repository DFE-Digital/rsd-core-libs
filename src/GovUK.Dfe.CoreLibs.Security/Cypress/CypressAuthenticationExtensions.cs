using GovUK.Dfe.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.Security.Cypress
{
    public static class CypressAuthenticationExtensions
    {
        /// <summary>
        /// Adds a PolicyScheme ("MultiAuth" by default) that checks for the Cypress request
        /// and either forwards to the "cypressScheme" or the "fallbackScheme".
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> from AddAuthentication().</param>
        /// <param name="policyScheme">Name of the policy scheme (default "MultiAuth").</param>
        /// <param name="displayName">Display name for the policy scheme in UI.</param>
        /// <param name="cypressScheme">The scheme to forward to if it's a valid Cypress request (default "CypressAuth").</param>
        /// <param name="fallbackScheme">The scheme to fallback to if not valid (default CookieAuthenticationDefaults.AuthenticationScheme).</param>
        public static AuthenticationBuilder AddCypressMultiAuthentication(
            this AuthenticationBuilder builder,
            string policyScheme = "MultiAuth",
            string displayName = "Multi Auth",
            string cypressScheme = "CypressAuth",
            string? fallbackScheme = null)
        {
            // Default fallback scheme to Cookies if not provided
            if (string.IsNullOrEmpty(fallbackScheme))
            {
                fallbackScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }

            // Ensure our CypressRequestChecker is registered
            builder.Services.AddScoped<ICustomRequestChecker, CypressRequestChecker>();

            builder.AddPolicyScheme(policyScheme, displayName, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var checker = context.RequestServices.GetRequiredService<ICustomRequestChecker>();

                    var isCypress = checker.IsValidRequest(context);

                    if (isCypress)
                    {
                        return cypressScheme;
                    }

                    // Otherwise fallback
                    return fallbackScheme;
                };
            });

            // Add the custom scheme
            builder.AddScheme<AuthenticationSchemeOptions, CypressAuthenticationHandler>(cypressScheme, _ => { });

            return builder;
        }
    }
}
