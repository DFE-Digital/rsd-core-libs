using DfE.CoreLibs.Security.Models;
using System.Security.Claims;

namespace DfE.CoreLibs.Security.Interfaces
{
    /// <summary>
    /// Interface for acquiring API access tokens.
    /// </summary>
    public interface IUserTokenService
    {
        /// <summary>
        /// Retrieves a custom JWT token for the specified authenticated user.
        /// If a valid token exists in the cache, it returns the cached token;
        /// otherwise, it generates a new token, caches it, and returns it.
        /// </summary>
        /// <param name="user">The authenticated user for whom the token is to be generated.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the JWT token as a string.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the user does not have a valid identifier.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="user"/> is null.</exception>
        Task<string> GetUserTokenAsync(ClaimsPrincipal user);

        /// <summary>
        /// Retrieves a custom JWT token for the specified authenticated user.
        /// If a valid token exists in the cache, it returns the cached token;
        /// otherwise, it generates a new token, caches it, and returns it.
        /// </summary>
        /// <param name="user">The authenticated user for whom the token is to be generated.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the token model object.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the user does not have a valid identifier.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="user"/> is null.</exception>
        Task<Token> GetUserTokenModelAsync(ClaimsPrincipal user);

    }
}
