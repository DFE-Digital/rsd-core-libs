﻿namespace DfE.CoreLibs.Security.Configurations
{
    /// <summary>
    /// Configuration for OpenID Connect integration.
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

        // Token validation
        public string? Issuer { get; set; }
        public string? JwksUri { get; set; }
        public string? DiscoveryEndpoint { get; set; }
        public bool ValidateIssuer { get; set; } = true;
        public bool ValidateAudience { get; set; } = false;
        public bool ValidateLifetime { get; set; } = true;

        public IList<string> Scopes { get; set; } = new List<string>();
    }
}