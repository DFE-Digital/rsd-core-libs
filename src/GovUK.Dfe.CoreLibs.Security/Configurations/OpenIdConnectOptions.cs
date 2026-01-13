using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GovUK.Dfe.CoreLibs.Security.Tests")]
namespace GovUK.Dfe.CoreLibs.Security.Configurations
{
    /// <summary>
    /// Configuration for OpenID Connect integration.
    /// Supports both single-provider and multi-provider (multi-tenant) scenarios.
    /// </summary>
    public class OpenIdConnectOptions
    {
        public string Authority { get; set; } = default!;
        public string ClientId { get; set; } = default!;
        public string ClientSecret { get; set; } = default!;
        public string? RedirectUri { get; set; }
        public string Prompt { get; set; } = "login";
        public string ResponseType { get; set; } = "code";
        public bool RequireHttpsMetadata { get; set; } = true;
        public bool GetClaimsFromUserInfoEndpoint { get; set; } = true;
        public bool SaveTokens { get; set; } = true;
        public bool UseTokenLifetime { get; set; } = true;
        public string NameClaimType { get; set; } = "email";

        // Token validation - single values (for backward compatibility)
        public string? Issuer { get; set; }
        public string? JwksUri { get; set; }
        public string? DiscoveryEndpoint { get; set; }
        public bool ValidateIssuer { get; set; } = true;
        public bool ValidateAudience { get; set; } = false;
        public bool ValidateLifetime { get; set; } = true;

        public IList<string> Scopes { get; set; } = new List<string>();

        // Multi-provider support - arrays for multiple OIDC providers

        /// <summary>
        /// List of valid issuers for token validation. 
        /// If populated, takes precedence over <see cref="Issuer"/>.
        /// Use for multi-tenant scenarios where tokens may come from different providers.
        /// </summary>
        public IList<string>? ValidIssuers { get; set; }

        /// <summary>
        /// List of valid audiences (client IDs) for token validation.
        /// If populated, takes precedence over <see cref="ClientId"/> for audience validation.
        /// Use for multi-tenant scenarios with different client registrations.
        /// </summary>
        public IList<string>? ValidAudiences { get; set; }

        /// <summary>
        /// List of discovery endpoints to fetch OIDC metadata from.
        /// If populated, takes precedence over <see cref="DiscoveryEndpoint"/>.
        /// Signing keys will be collected from ALL endpoints.
        /// Use for multi-tenant scenarios with multiple OIDC providers.
        /// </summary>
        public IList<string>? DiscoveryEndpoints { get; set; }

        /// <summary>
        /// Gets all configured discovery endpoints (both single and array values).
        /// </summary>
        internal IEnumerable<string> GetAllDiscoveryEndpoints()
        {
            if (DiscoveryEndpoints?.Any() == true)
            {
                foreach (var endpoint in DiscoveryEndpoints.Where(e => !string.IsNullOrEmpty(e)))
                    yield return endpoint;
            }
            else if (!string.IsNullOrEmpty(DiscoveryEndpoint))
            {
                yield return DiscoveryEndpoint;
            }
        }

        /// <summary>
        /// Gets all configured valid issuers (both single and array values).
        /// </summary>
        internal IEnumerable<string> GetAllValidIssuers()
        {
            if (ValidIssuers?.Any() == true)
            {
                foreach (var issuer in ValidIssuers.Where(i => !string.IsNullOrEmpty(i)))
                    yield return issuer;
            }
            else if (!string.IsNullOrEmpty(Issuer))
            {
                yield return Issuer;
            }
        }

        /// <summary>
        /// Gets all configured valid audiences (both single and array values).
        /// </summary>
        internal IEnumerable<string> GetAllValidAudiences()
        {
            if (ValidAudiences?.Any() == true)
            {
                foreach (var audience in ValidAudiences.Where(a => !string.IsNullOrEmpty(a)))
                    yield return audience;
            }
            else if (!string.IsNullOrEmpty(ClientId))
            {
                yield return ClientId;
            }
        }
    }
}