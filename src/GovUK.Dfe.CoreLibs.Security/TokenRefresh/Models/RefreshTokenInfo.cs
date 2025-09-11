namespace GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models
{
    /// <summary>
    /// Represents information about a refresh token, including its validity and metadata.
    /// This model is used internally to track refresh token state and determine when refreshes are needed.
    /// </summary>
    public class RefreshTokenInfo
    {
        /// <summary>
        /// Gets or sets the refresh token value.
        /// </summary>
        public string RefreshToken { get; set; } = default!;

        /// <summary>
        /// Gets or sets a value indicating whether the refresh token is valid and active.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the time when the refresh token expires.
        /// Some providers include expiration information for refresh tokens.
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the scope associated with the refresh token.
        /// This represents the permissions that can be granted when using this refresh token.
        /// </summary>
        public string? Scope { get; set; }

        /// <summary>
        /// Gets or sets the client ID for which this refresh token was issued.
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Gets or sets the subject (user) identifier associated with this refresh token.
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the time when this refresh token information was last verified.
        /// This can be used to implement caching strategies and avoid unnecessary introspection calls.
        /// </summary>
        public DateTimeOffset LastVerified { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets additional metadata about the refresh token.
        /// This allows for provider-specific information to be stored.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Gets a value indicating whether the refresh token has expired.
        /// Returns true if the token has an expiration time and it has passed.
        /// </summary>
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets a value indicating whether the refresh token information needs to be re-verified.
        /// This is based on a configurable cache duration to avoid excessive introspection calls.
        /// </summary>
        /// <param name="cacheLifetime">The lifetime of the cached token information.</param>
        /// <returns>True if the token information should be re-verified.</returns>
        public bool NeedsReverification(TimeSpan cacheLifetime)
        {
            return DateTimeOffset.UtcNow - LastVerified > cacheLifetime;
        }

        /// <summary>
        /// Creates a new <see cref="RefreshTokenInfo"/> from a token introspection response.
        /// </summary>
        /// <param name="refreshToken">The refresh token value.</param>
        /// <param name="introspectionResponse">The introspection response containing token metadata.</param>
        /// <returns>A new <see cref="RefreshTokenInfo"/> instance.</returns>
        public static RefreshTokenInfo FromIntrospectionResponse(string refreshToken, TokenIntrospectionResponse introspectionResponse)
        {
            return new RefreshTokenInfo
            {
                RefreshToken = refreshToken,
                IsValid = introspectionResponse.Active,
                ExpiresAt = introspectionResponse.ExpiresAt,
                Scope = introspectionResponse.Scope,
                ClientId = introspectionResponse.ClientId,
                Subject = introspectionResponse.Subject,
                LastVerified = DateTimeOffset.UtcNow,
                Metadata = introspectionResponse.AdditionalClaims ?? new Dictionary<string, object>()
            };
        }
    }
}
