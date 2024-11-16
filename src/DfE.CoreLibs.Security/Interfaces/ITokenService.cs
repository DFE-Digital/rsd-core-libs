using System.Security.Claims;

namespace DfE.CoreLibs.Security.Interfaces
{
    /// <summary>
    /// Interface for acquiring API access tokens.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Acquires an API access token by mapping the user's roles to API scopes/permissions using the On-Behalf-Of (OBO) flow.
        /// </summary>
        /// <param name="authenticationScheme">The authentication scheme.</param>
        /// <returns>A JWT access token string for accessing the API.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user does not have any roles or if no valid scopes are found for the user's roles.</exception>
        Task<string> GetApiOboTokenAsync(string? authenticationScheme = null);

        /// <summary>
        /// ----
        /// DO NOT USE THIS METHOD IF YOUR API IS RUNNING IN A SEPARATE SERVICE.
        /// PLEASE FOLLOW On-Behalf-Of (OBO) FLOW BY USING GetApiOboTokenAsync METHOD.
        /// ----
        /// Retrieves a custom JWT token for the specified authenticated user.
        /// If a valid token exists in the cache, it returns the cached token;
        /// otherwise, it generates a new token, caches it, and returns it.
        /// </summary>
        /// <param name="user">The authenticated user for whom the token is to be generated.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the JWT token as a string.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the user does not have a valid identifier.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="user"/> is null.</exception>
        Task<string> GetUserTokenAsync(ClaimsPrincipal user);

    }
}
