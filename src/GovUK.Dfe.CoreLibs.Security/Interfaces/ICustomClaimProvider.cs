using System.Security.Claims;

namespace GovUK.Dfe.CoreLibs.Security.Interfaces
{
    /// <summary>
    /// Interface for custom claim providers that retrieve claims for a user.
    /// </summary>
    public interface ICustomClaimProvider
    {
        /// <summary>
        /// Asynchronously retrieves claims for the specified user.
        /// </summary>
        /// <param name="principal">The current user's ClaimsPrincipal.</param>
        /// <returns>A list of claims to add to the user's identity.</returns>
        Task<IEnumerable<Claim>> GetClaimsAsync(ClaimsPrincipal principal);
    }
}
