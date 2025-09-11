using GovUK.Dfe.CoreLibs.Security.Models;
using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models
{
    /// <summary>
    /// Represents the response from a token refresh operation.
    /// This model encapsulates both the new token information and metadata about the refresh operation.
    /// </summary>
    public class TokenRefreshResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the token refresh operation was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the refreshed token information.
        /// This will be null if the refresh operation failed.
        /// </summary>
        public Token? Token { get; set; }

        /// <summary>
        /// Gets or sets an error message if the refresh operation failed.
        /// This will be null if the operation was successful.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the error code from the OAuth 2.0 provider if the refresh operation failed.
        /// Common error codes include "invalid_grant", "invalid_client", etc.
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the refresh operation was performed.
        /// </summary>
        public DateTimeOffset RefreshedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets a value indicating whether the refresh token was rotated (changed) during the refresh.
        /// Some providers rotate refresh tokens on each use for security reasons.
        /// </summary>
        public bool RefreshTokenRotated { get; set; }

        /// <summary>
        /// Gets or sets the time when the new access token will expire.
        /// This is calculated based on the expires_in value from the token response.
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }

        /// <summary>
        /// Creates a successful token refresh response.
        /// </summary>
        /// <param name="token">The refreshed token.</param>
        /// <param name="refreshTokenRotated">Whether the refresh token was rotated.</param>
        /// <returns>A successful <see cref="TokenRefreshResponse"/>.</returns>
        public static TokenRefreshResponse Success(Token token, bool refreshTokenRotated = false)
        {
            var expiresAt = token.ExpiresIn > 0 
                ? DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn) 
                : (DateTimeOffset?)null;

            return new TokenRefreshResponse
            {
                IsSuccess = true,
                Token = token,
                RefreshTokenRotated = refreshTokenRotated,
                ExpiresAt = expiresAt,
                RefreshedAt = DateTimeOffset.UtcNow
            };
        }

        /// <summary>
        /// Creates a failed token refresh response.
        /// </summary>
        /// <param name="errorMessage">The error message describing the failure.</param>
        /// <param name="errorCode">The OAuth 2.0 error code.</param>
        /// <returns>A failed <see cref="TokenRefreshResponse"/>.</returns>
        public static TokenRefreshResponse Failure(string errorMessage, string? errorCode = null)
        {
            return new TokenRefreshResponse
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode,
                RefreshedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
