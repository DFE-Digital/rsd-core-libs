using GovUK.Dfe.CoreLibs.Security.Models;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Configuration;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Exceptions;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Interfaces;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.Security.TokenRefresh.Services
{
    /// <summary>
    /// Implementation of <see cref="ITokenRefreshService"/> that provides high-level token refresh functionality.
    /// This service orchestrates token refresh operations using the configured provider and handles
    /// refresh logic, caching, and automatic token management.
    /// </summary>
    public class TokenRefreshService : ITokenRefreshService
    {
        private readonly ITokenRefreshProvider _tokenRefreshProvider;
        private readonly ITokenIntrospectionService _tokenIntrospectionService;
        private readonly TokenRefreshOptions _options;
        private readonly ILogger<TokenRefreshService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenRefreshService"/> class.
        /// </summary>
        /// <param name="tokenRefreshProvider">The provider for token refresh operations.</param>
        /// <param name="tokenIntrospectionService">The service for token introspection operations.</param>
        /// <param name="options">The token refresh configuration options.</param>
        /// <param name="logger">The logger for this service.</param>
        public TokenRefreshService(
            ITokenRefreshProvider tokenRefreshProvider,
            ITokenIntrospectionService tokenIntrospectionService,
            IOptions<TokenRefreshOptions> options,
            ILogger<TokenRefreshService> logger)
        {
            _tokenRefreshProvider = tokenRefreshProvider ?? throw new ArgumentNullException(nameof(tokenRefreshProvider));
            _tokenIntrospectionService = tokenIntrospectionService ?? throw new ArgumentNullException(nameof(tokenIntrospectionService));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _options.Validate();
        }

        /// <inheritdoc/>
        public async Task<TokenRefreshResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentNullException(nameof(refreshToken));

            _logger.LogDebug("Starting token refresh operation");

            try
            {
                var request = new TokenRefreshRequest
                {
                    RefreshToken = refreshToken,
                    ClientId = _options.ClientId,
                    ClientSecret = _options.ClientSecret,
                    Scope = _options.DefaultScope,
                    TokenEndpoint = _options.TokenEndpoint
                };

                var response = await _tokenRefreshProvider.RefreshTokenAsync(request, cancellationToken);

                if (response.IsSuccess)
                {
                    _logger.LogInformation("Token refresh completed successfully");
                }
                else
                {
                    _logger.LogWarning("Token refresh failed: {ErrorMessage}", response.ErrorMessage);
                }

                return response;
            }
            catch (Exception ex) when (!(ex is TokenRefreshException))
            {
                _logger.LogError(ex, "Unexpected error during token refresh");
                throw new TokenRefreshException("Unexpected error during token refresh", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsRefreshTokenValidAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentNullException(nameof(refreshToken));

            _logger.LogDebug("Checking refresh token validity");

            try
            {
                return await _tokenIntrospectionService.IsTokenActiveAsync(refreshToken, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate refresh token, assuming invalid");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<TokenRefreshResponse> RefreshTokenIfNeededAsync(Token currentToken, CancellationToken cancellationToken = default)
        {
            if (currentToken == null)
                throw new ArgumentNullException(nameof(currentToken));

            if (string.IsNullOrWhiteSpace(currentToken.RefreshToken))
                throw new InvalidOperationException("Current token does not contain a refresh token");

            _logger.LogDebug("Checking if token refresh is needed");

            if (!ShouldRefreshToken(currentToken))
            {
                _logger.LogDebug("Token refresh not needed, token is still valid");
                return TokenRefreshResponse.Success(currentToken, false);
            }

            _logger.LogDebug("Token refresh is needed, proceeding with refresh");
            return await RefreshTokenAsync(currentToken.RefreshToken, cancellationToken);
        }

        /// <inheritdoc/>
        public bool ShouldRefreshToken(Token token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            // If we don't have expiration information, we can't determine if refresh is needed
            if (token.ExpiresIn <= 0)
            {
                _logger.LogDebug("Token does not have expiration information, refresh determination not possible");
                return false;
            }

            // ExpiresIn is the number of seconds until expiration from now
            // So the token will expire at: now + ExpiresIn seconds
            var expirationTime = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
            var refreshTime = expirationTime.Subtract(_options.RefreshBuffer);

            var shouldRefresh = DateTimeOffset.UtcNow >= refreshTime;

            _logger.LogDebug("Token refresh check: Current={Current}, Expires={Expires}, Refresh={Refresh}, Should={Should}", 
                DateTimeOffset.UtcNow, expirationTime, refreshTime, shouldRefresh);

            return shouldRefresh;
        }

        /// <summary>
        /// Determines if a token should be refreshed based on a known issue time.
        /// This method provides more accurate refresh timing when the token issue time is known.
        /// </summary>
        /// <param name="token">The token to check.</param>
        /// <param name="issuedAt">The time when the token was issued.</param>
        /// <returns>True if the token should be refreshed, false otherwise.</returns>
        public bool ShouldRefreshToken(Token token, DateTimeOffset issuedAt)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            // If we don't have expiration information, we can't determine if refresh is needed
            if (token.ExpiresIn <= 0)
            {
                _logger.LogDebug("Token does not have expiration information, refresh determination not possible");
                return false;
            }

            var expirationTime = issuedAt.AddSeconds(token.ExpiresIn);
            var refreshTime = expirationTime.Subtract(_options.RefreshBuffer);

            var shouldRefresh = DateTimeOffset.UtcNow >= refreshTime;

            _logger.LogDebug("Token refresh check with known issue time: Current={Current}, Issued={Issued}, Expires={Expires}, Refresh={Refresh}, Should={Should}", 
                DateTimeOffset.UtcNow, issuedAt, expirationTime, refreshTime, shouldRefresh);

            return shouldRefresh;
        }

        /// <summary>
        /// Performs token refresh with retry logic for transient failures.
        /// </summary>
        /// <param name="refreshToken">The refresh token to use.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A <see cref="TokenRefreshResponse"/> containing the result of the refresh operation.</returns>
        public async Task<TokenRefreshResponse> RefreshTokenWithRetryAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentNullException(nameof(refreshToken));

            var maxAttempts = _options.MaxRetryAttempts + 1; // +1 for the initial attempt
            var attempt = 0;

            while (attempt < maxAttempts)
            {
                attempt++;
                
                try
                {
                    _logger.LogDebug("Token refresh attempt {Attempt} of {MaxAttempts}", attempt, maxAttempts);
                    
                    var response = await RefreshTokenAsync(refreshToken, cancellationToken);
                    
                    if (response.IsSuccess)
                    {
                        return response;
                    }

                    // Check if this is a non-retryable error
                    if (IsNonRetryableError(response.ErrorCode))
                    {
                        _logger.LogWarning("Non-retryable error encountered: {ErrorCode}", response.ErrorCode);
                        return response;
                    }

                    // If this was the last attempt, return the failure
                    if (attempt >= maxAttempts)
                    {
                        return response;
                    }
                }
                catch (TokenRefreshException ex) when (IsRetryableException(ex))
                {
                    _logger.LogWarning(ex, "Retryable error on attempt {Attempt}: {Message}", attempt, ex.Message);
                    
                    // If this was the last attempt, re-throw
                    if (attempt >= maxAttempts)
                    {
                        throw;
                    }
                }

                // Wait before retrying (exponential backoff)
                if (attempt < maxAttempts)
                {
                    var delay = TimeSpan.FromMilliseconds(_options.RetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                    _logger.LogDebug("Waiting {Delay}ms before retry attempt {NextAttempt}", delay.TotalMilliseconds, attempt + 1);
                    await Task.Delay(delay, cancellationToken);
                }
            }

            // This should never be reached, but return a failure response as a fallback
            return TokenRefreshResponse.Failure("Maximum retry attempts exceeded");
        }

        private static bool IsNonRetryableError(string? errorCode)
        {
            // These OAuth 2.0 error codes indicate permanent failures that shouldn't be retried
            return errorCode switch
            {
                "invalid_grant" => true,      // Refresh token is expired or revoked
                "invalid_client" => true,     // Client authentication failed
                "unauthorized_client" => true, // Client not authorized for this grant type
                "unsupported_grant_type" => true, // Server doesn't support refresh tokens
                _ => false
            };
        }

        private static bool IsRetryableException(TokenRefreshException ex)
        {
            // Retry on HTTP-level errors but not on authentication/authorization errors
            return ex.StatusCode.HasValue && ex.StatusCode.Value >= 500;
        }
    }
}
