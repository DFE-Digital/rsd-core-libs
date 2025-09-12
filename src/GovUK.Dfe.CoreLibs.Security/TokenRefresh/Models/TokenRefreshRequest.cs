namespace GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models
{
    /// <summary>
    /// Represents a request to refresh an access token using a refresh token.
    /// This model encapsulates all the information needed to make a token refresh request
    /// to an OAuth 2.0 / OpenID Connect provider.
    /// </summary>
    public class TokenRefreshRequest
    {
        /// <summary>
        /// Gets or sets the refresh token used to obtain a new access token.
        /// This is the refresh_token value from the original token response.
        /// </summary>
        public string RefreshToken { get; set; } = default!;

        /// <summary>
        /// Gets or sets the client ID for the application requesting the token refresh.
        /// This should match the client_id used in the original authorization request.
        /// </summary>
        public string ClientId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the client secret for the application.
        /// Required for confidential clients when using client_secret_post authentication.
        /// </summary>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the scope to request for the new access token.
        /// If null or empty, the authorization server will issue a token with the same scope as the original.
        /// If specified, it must be equal to or a subset of the scope originally granted.
        /// </summary>
        public string? Scope { get; set; }

        /// <summary>
        /// Gets or sets additional parameters that may be required by specific providers.
        /// This allows for provider-specific extensions while maintaining compatibility.
        /// </summary>
        public Dictionary<string, string> AdditionalParameters { get; set; } = new();

        /// <summary>
        /// Gets or sets the token endpoint URL where the refresh request will be sent.
        /// This is typically obtained from the provider's discovery document.
        /// </summary>
        public string TokenEndpoint { get; set; } = default!;
    }
}
