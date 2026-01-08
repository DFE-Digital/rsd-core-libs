namespace GovUK.Dfe.CoreLibs.Security.Configurations
{
    /// <summary>
    /// Configuration options for multi-tenant scenarios with multiple isolated OIDC providers.
    /// Each provider is a complete, isolated configuration - tokens must match ALL criteria
    /// of a single provider to be valid (no cross-provider validation).
    /// </summary>
    public class MultiProviderOpenIdConnectOptions
    {
        /// <summary>
        /// List of OIDC provider configurations. Each represents a completely
        /// isolated tenant/organization with its own issuer, audience, and discovery endpoint.
        /// </summary>
        public List<OpenIdConnectOptions> Providers { get; set; } = [];
    }
}