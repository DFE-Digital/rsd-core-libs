using System.Security.Claims;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Authentication;

namespace DfE.CoreLibs.Security.Authorization
{
    /// <summary>
    /// Transforms the claims of the current user by adding custom claims from registered claim providers.
    /// </summary>
    public class CustomClaimsTransformation(IEnumerable<ICustomClaimProvider> claimProviders) : IClaimsTransformation
    {
        /// <summary>
        /// Transforms the user's claims by adding custom claims retrieved from each registered claim provider.
        /// </summary>
        /// <param name="principal">The current user's ClaimsPrincipal.</param>
        /// <returns>The modified ClaimsPrincipal with additional claims.</returns>
        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var identity = (ClaimsIdentity)principal.Identity!;

            foreach (var provider in claimProviders)
            {
                var claims = await provider.GetClaimsAsync(principal);

                foreach (var claim in claims)
                {
                    if (!identity.HasClaim(c => c.Type == claim.Type && c.Value == claim.Value))
                    {
                        identity.AddClaim(claim);
                    }
                }
            }

            return principal;
        }
    }
}
