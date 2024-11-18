namespace DfE.CoreLibs.Security.Interfaces
{
    /// <summary>
    /// Interface for acquiring API On-Behalf-Of (OBO) access tokens.
    /// </summary>
    public interface IApiOboTokenService
    {
        /// <summary>
        /// Acquires an API access token by mapping the user's roles to API scopes/permissions using the On-Behalf-Of (OBO) flow.
        /// </summary>
        /// <param name="authenticationScheme">The authentication scheme.</param>
        /// <returns>A JWT access token string for accessing the API.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user does not have any roles or if no valid scopes are found for the user's roles.</exception>
        Task<string> GetApiOboTokenAsync(string? authenticationScheme = null);
    }
}
