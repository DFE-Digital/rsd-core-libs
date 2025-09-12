using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models;

namespace GovUK.Dfe.CoreLibs.Security.TokenRefresh.Interfaces
{
    /// <summary>
    /// Defines a contract for provider-specific token refresh operations.
    /// This interface allows different OIDC providers to implement their own token refresh logic
    /// while maintaining a consistent interface for the core token refresh service.
    /// </summary>
    public interface ITokenRefreshProvider
    {
        /// <summary>
        /// Refreshes an access token using provider-specific logic.
        /// This method handles the actual HTTP communication with the provider's token endpoint.
        /// </summary>
        /// <param name="request">The token refresh request containing the refresh token and any provider-specific parameters.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A <see cref="TokenRefreshResponse"/> containing the new access token and related information.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
        /// <exception cref="TokenRefreshException">Thrown when the token refresh operation fails.</exception>
        Task<TokenRefreshResponse> RefreshTokenAsync(TokenRefreshRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Introspects a token using provider-specific logic.
        /// This method handles the actual HTTP communication with the provider's introspection endpoint.
        /// </summary>
        /// <param name="token">The token to introspect.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A <see cref="TokenIntrospectionResponse"/> containing information about the token.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="token"/> is null or empty.</exception>
        /// <exception cref="TokenIntrospectionException">Thrown when the introspection operation fails.</exception>
        Task<TokenIntrospectionResponse> IntrospectTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the name of the provider (e.g., "DfESignIn", "AzureAD", etc.).
        /// This can be used for logging and configuration purposes.
        /// </summary>
        string ProviderName { get; }
    }
}
