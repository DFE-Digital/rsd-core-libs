namespace DfE.CoreLibs.Security.Configurations
{
    /// <summary>
    /// Configuration for the DfE Sign-In “ID-only” OpenID Connect integration.
    /// </summary>
    public class DfESignInOptions
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
        public IList<string> Scopes { get; set; } = new List<string>();
    }
}