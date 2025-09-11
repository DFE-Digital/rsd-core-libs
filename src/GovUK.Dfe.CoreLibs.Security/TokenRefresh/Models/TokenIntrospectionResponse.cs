using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models
{
    /// <summary>
    /// Represents the response from an OAuth 2.0 token introspection endpoint (RFC 7662).
    /// This model contains information about a token's validity and metadata.
    /// </summary>
    public class TokenIntrospectionResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the token is active.
        /// This is the primary field that indicates if the token is valid and has not expired.
        /// </summary>
        [JsonPropertyName("active")]
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the subject of the token (usually the user identifier).
        /// This field is present when the token is active.
        /// </summary>
        [JsonPropertyName("sub")]
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the client identifier for which the token was issued.
        /// </summary>
        [JsonPropertyName("client_id")]
        public string? ClientId { get; set; }

        /// <summary>
        /// Gets or sets the expiration time of the token as a Unix timestamp.
        /// This field is present when the token is active.
        /// </summary>
        [JsonPropertyName("exp")]
        public long? ExpirationTime { get; set; }

        /// <summary>
        /// Gets or sets the time at which the token was issued as a Unix timestamp.
        /// </summary>
        [JsonPropertyName("iat")]
        public long? IssuedAt { get; set; }

        /// <summary>
        /// Gets or sets the issuer of the token.
        /// This identifies the authorization server that issued the token.
        /// </summary>
        [JsonPropertyName("iss")]
        public string? Issuer { get; set; }

        /// <summary>
        /// Gets or sets the scope associated with the token.
        /// This is a space-separated list of scope values.
        /// </summary>
        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        /// <summary>
        /// Gets or sets the token type (e.g., "Bearer").
        /// </summary>
        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        /// <summary>
        /// Gets or sets the username associated with the token.
        /// This is an optional field that may be present depending on the token type.
        /// </summary>
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets additional claims or metadata about the token.
        /// This allows for provider-specific extensions.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalClaims { get; set; }

        /// <summary>
        /// Gets the expiration time as a <see cref="DateTimeOffset"/>.
        /// Returns null if the expiration time is not set.
        /// </summary>
        [JsonIgnore]
        public DateTimeOffset? ExpiresAt => ExpirationTime.HasValue 
            ? DateTimeOffset.FromUnixTimeSeconds(ExpirationTime.Value) 
            : null;

        /// <summary>
        /// Gets the issued at time as a <see cref="DateTimeOffset"/>.
        /// Returns null if the issued at time is not set.
        /// </summary>
        [JsonIgnore]
        public DateTimeOffset? IssuedAtTime => IssuedAt.HasValue 
            ? DateTimeOffset.FromUnixTimeSeconds(IssuedAt.Value) 
            : null;

        /// <summary>
        /// Gets a value indicating whether the token has expired.
        /// Returns true if the token has an expiration time and it has passed.
        /// </summary>
        [JsonIgnore]
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets the scopes as an array of individual scope values.
        /// Returns an empty array if no scopes are present.
        /// </summary>
        [JsonIgnore]
        public string[] Scopes => string.IsNullOrWhiteSpace(Scope) 
            ? Array.Empty<string>() 
            : Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}
