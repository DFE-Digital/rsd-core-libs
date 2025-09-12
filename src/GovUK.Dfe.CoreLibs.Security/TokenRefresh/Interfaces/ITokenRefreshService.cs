using GovUK.Dfe.CoreLibs.Security.Models;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models;

namespace GovUK.Dfe.CoreLibs.Security.TokenRefresh.Interfaces
{
    /// <summary>
    /// Defines a contract for token refresh operations, providing functionality to refresh access tokens
    /// using refresh tokens and automatically manage token lifecycle.
    /// </summary>
    public interface ITokenRefreshService
    {
        /// <summary>
        /// Refreshes an access token using the provided refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token to use for obtaining a new access token.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A <see cref="TokenRefreshResponse"/> containing the new access token and related information.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="refreshToken"/> is null or empty.</exception>
        /// <exception cref="TokenRefreshException">Thrown when the token refresh operation fails.</exception>
        Task<TokenRefreshResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a refresh token is still valid by introspecting it with the authorization server.
        /// </summary>
        /// <param name="refreshToken">The refresh token to validate.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>True if the refresh token is valid and active, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="refreshToken"/> is null or empty.</exception>
        Task<bool> IsRefreshTokenValidAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes the access token if it's close to expiration or if explicitly needed.
        /// This method checks the current token's expiration time and automatically refreshes it
        /// if it's within the configured buffer time before expiry.
        /// </summary>
        /// <param name="currentToken">The current token containing both access and refresh tokens.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="TokenRefreshResponse"/> containing either the refreshed token if renewal was needed,
        /// or the original token information if no refresh was required.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="currentToken"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the current token doesn't contain a refresh token.</exception>
        /// <exception cref="TokenRefreshException">Thrown when the token refresh operation fails.</exception>
        Task<TokenRefreshResponse> RefreshTokenIfNeededAsync(Token currentToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines if a token needs to be refreshed based on its expiration time and the configured buffer.
        /// </summary>
        /// <param name="token">The token to check for refresh necessity.</param>
        /// <returns>True if the token should be refreshed, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="token"/> is null.</exception>
        bool ShouldRefreshToken(Token token);
    }
}
