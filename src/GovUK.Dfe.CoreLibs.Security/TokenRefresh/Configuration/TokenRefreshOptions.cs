namespace GovUK.Dfe.CoreLibs.Security.TokenRefresh.Configuration
{
    /// <summary>
    /// Configuration options for token refresh operations.
    /// This class contains settings that control how token refresh operations are performed.
    /// </summary>
    public class TokenRefreshOptions
    {
        /// <summary>
        /// Gets or sets the OAuth 2.0 token endpoint URL.
        /// This is the endpoint where token refresh requests will be sent.
        /// </summary>
        public string TokenEndpoint { get; set; } = default!;

        /// <summary>
        /// Gets or sets the OAuth 2.0 token introspection endpoint URL.
        /// This is the endpoint where token introspection requests will be sent to validate tokens.
        /// </summary>
        public string IntrospectionEndpoint { get; set; } = default!;

        /// <summary>
        /// Gets or sets the client ID for the application.
        /// This is used in token refresh and introspection requests.
        /// </summary>
        public string ClientId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the client secret for the application.
        /// This is used for client authentication in token refresh and introspection requests.
        /// </summary>
        public string ClientSecret { get; set; } = default!;

        /// <summary>
        /// Gets or sets the number of minutes before token expiration when a refresh should be attempted.
        /// Default is 5 minutes, meaning tokens will be refreshed when they have 5 minutes or less remaining.
        /// </summary>
        public int RefreshBufferMinutes { get; set; } = 5;

        /// <summary>
        /// Gets or sets a value indicating whether background token refresh is enabled.
        /// When enabled, the service can proactively refresh tokens before they expire.
        /// Default is true.
        /// </summary>
        public bool EnableBackgroundRefresh { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval at which background refresh checks are performed.
        /// This is only used when EnableBackgroundRefresh is true.
        /// Default is 1 minute.
        /// </summary>
        public TimeSpan RefreshCheckInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the timeout for HTTP requests to the token and introspection endpoints.
        /// Default is 30 seconds.
        /// </summary>
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the number of retry attempts for failed HTTP requests.
        /// Default is 3 attempts.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the base delay between retry attempts.
        /// The actual delay may be longer due to exponential backoff.
        /// Default is 1 second.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the cache lifetime for token introspection results.
        /// This helps reduce the number of introspection calls by caching results for a short period.
        /// Default is 5 minutes.
        /// </summary>
        public TimeSpan IntrospectionCacheLifetime { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets a value indicating whether to validate SSL certificates for HTTPS requests.
        /// This should only be set to false in development environments.
        /// Default is true.
        /// </summary>
        public bool ValidateSslCertificates { get; set; } = true;

        /// <summary>
        /// Gets or sets additional HTTP headers to include in token refresh and introspection requests.
        /// This can be used for provider-specific requirements.
        /// </summary>
        public Dictionary<string, string> AdditionalHeaders { get; set; } = new();

        /// <summary>
        /// Gets or sets the name of the HTTP client to use for token operations.
        /// This allows for custom HTTP client configurations via IHttpClientFactory.
        /// If not specified, a default HTTP client will be used.
        /// </summary>
        public string? HttpClientName { get; set; }

        /// <summary>
        /// Gets or sets the scope to request when refreshing tokens.
        /// If null or empty, the original scope will be maintained.
        /// If specified, it must be equal to or a subset of the originally granted scope.
        /// </summary>
        public string? DefaultScope { get; set; }

        /// <summary>
        /// Gets the refresh buffer as a TimeSpan.
        /// </summary>
        public TimeSpan RefreshBuffer => TimeSpan.FromMinutes(RefreshBufferMinutes);

        /// <summary>
        /// Validates the configuration options and throws an exception if any required values are missing or invalid.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when required configuration values are missing or invalid.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(TokenEndpoint))
                throw new InvalidOperationException("TokenEndpoint is required and cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(IntrospectionEndpoint))
                throw new InvalidOperationException("IntrospectionEndpoint is required and cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(ClientId))
                throw new InvalidOperationException("ClientId is required and cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(ClientSecret))
                throw new InvalidOperationException("ClientSecret is required and cannot be null or empty.");

            if (!Uri.TryCreate(TokenEndpoint, UriKind.Absolute, out _))
                throw new InvalidOperationException("TokenEndpoint must be a valid absolute URI.");

            if (!Uri.TryCreate(IntrospectionEndpoint, UriKind.Absolute, out _))
                throw new InvalidOperationException("IntrospectionEndpoint must be a valid absolute URI.");

            if (RefreshBufferMinutes < 0)
                throw new InvalidOperationException("RefreshBufferMinutes cannot be negative.");

            if (HttpTimeout <= TimeSpan.Zero)
                throw new InvalidOperationException("HttpTimeout must be greater than zero.");

            if (MaxRetryAttempts < 0)
                throw new InvalidOperationException("MaxRetryAttempts cannot be negative.");

            if (RetryDelay < TimeSpan.Zero)
                throw new InvalidOperationException("RetryDelay cannot be negative.");

            if (IntrospectionCacheLifetime <= TimeSpan.Zero)
                throw new InvalidOperationException("IntrospectionCacheLifetime must be greater than zero.");
        }
    }
}
