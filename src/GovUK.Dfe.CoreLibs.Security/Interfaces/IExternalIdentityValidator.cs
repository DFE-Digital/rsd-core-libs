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
            CancellationToken cancellationToken = default);
    }
}
