using GovUK.Dfe.CoreLibs.Security.Configurations;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace GovUK.Dfe.CoreLibs.Security.Interfaces
{
    /// <summary>
    /// Defines a contract for validating OpenID Connect ID tokens and producing a <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public interface IExternalIdentityValidator
    {
        /// <summary>
        /// Validates the specified OpenID Connect ID token, fetching and caching the provider's metadata as needed.
        /// </summary>
        /// <param name="idToken">The raw JWT ID token to validate.</param>
        /// <param name="validCypressRequest">Whether this is a valid cypress request</param>
        /// <param name="validInternalRequest">Whether this is a valid internal request</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>
        /// A <see cref="ClaimsPrincipal"/> representing the validated token's claims.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="idToken"/> is <c>null</c> or empty.
        /// </exception>
        /// <exception cref="SecurityTokenValidationException">
        /// Thrown if the token fails signature, issuer, or lifetime validation.
        /// </exception>
        Task<ClaimsPrincipal> ValidateIdTokenAsync(
            string idToken,
            bool validCypressRequest = false,
            bool validInternalRequest = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates an ID token with optional override options for multi-tenant scenarios.
        /// </summary>
        /// <param name="idToken">The ID token to validate.</param>
        /// <param name="validCypressRequest">Whether this is a valid Cypress test request.</param>
        /// <param name="validInternalRequest">Whether this is a valid internal service request.</param>
        /// <param name="internalAuthOptions">
        /// Optional internal auth options to override the configured defaults.
        /// Use this for multi-tenant scenarios where each tenant has different internal auth settings.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The validated claims principal.</returns>
        Task<ClaimsPrincipal> ValidateIdTokenAsync(
            string idToken,
            bool validCypressRequest,
            bool validInternalRequest,
            InternalServiceAuthOptions? internalAuthOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates an ID token with tenant-specific authentication options for multi-tenant scenarios.
        /// Use this overload when both internal and test authentication settings differ per tenant.
        /// </summary>
        /// <param name="idToken">The ID token to validate.</param>
        /// <param name="validCypressRequest">Whether this is a valid Cypress test request.</param>
        /// <param name="validInternalRequest">Whether this is a valid internal service request.</param>
        /// <param name="internalAuthOptions">
        /// Optional internal auth options to override the configured defaults.
        /// </param>
        /// <param name="testAuthOptions">
        /// Optional test auth options to override the configured defaults.
        /// Use this for multi-tenant scenarios where each tenant has different test auth settings.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The validated claims principal.</returns>
        Task<ClaimsPrincipal> ValidateIdTokenAsync(
            string idToken,
            bool validCypressRequest,
            bool validInternalRequest,
            InternalServiceAuthOptions? internalAuthOptions,
            TestAuthenticationOptions? testAuthOptions,
            CancellationToken cancellationToken = default);
    }
}
