using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models;

namespace GovUK.Dfe.CoreLibs.Security.TokenRefresh.Interfaces
{
    /// <summary>
    /// Defines a contract for token introspection operations, allowing validation of tokens
    /// against an OAuth 2.0 introspection endpoint (RFC 7662).
    /// </summary>
    public interface ITokenIntrospectionService
    {
        /// <summary>
        /// Introspects a token to determine its validity and retrieve metadata about it.
        /// This method calls the OAuth 2.0 token introspection endpoint to check if a token
        /// is active and to retrieve information about the token.
        /// </summary>
        /// <param name="token">The token to introspect (can be access token, refresh token, etc.).</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="TokenIntrospectionResponse"/> containing information about the token,
        /// including whether it's active and its expiration time.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="token"/> is null or empty.</exception>
        /// <exception cref="TokenIntrospectionException">Thrown when the introspection operation fails.</exception>
        Task<TokenIntrospectionResponse> IntrospectTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a token is active by performing introspection.
        /// This is a convenience method that returns only the active status.
        /// </summary>
        /// <param name="token">The token to check for activity.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>True if the token is active, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="token"/> is null or empty.</exception>
        Task<bool> IsTokenActiveAsync(string token, CancellationToken cancellationToken = default);
    }
}
