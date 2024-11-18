using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace DfE.CoreLibs.Security.Authorization
{
    /// <summary>
    /// Provides functionality for acquiring API On-Behalf-Of (OBO) tokens for authenticated users.
    /// </summary>
    public class ApiOboTokenService : IApiOboTokenService
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiOboTokenService"/> class.
        /// </summary>
        /// <param name="tokenAcquisition">The token acquisition service for acquiring tokens.</param>
        /// <param name="httpContextAccessor">Accessor for the current HTTP context, used to retrieve the user's claims.</param>
        /// <param name="configuration">Configuration used to retrieve role-to-scope mappings.</param>
        public ApiOboTokenService(
            ITokenAcquisition tokenAcquisition,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
        {
            _tokenAcquisition = tokenAcquisition;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        /// <inheritdoc />
        public async Task<string> GetApiOboTokenAsync(string? authenticationScheme = null)
        {
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
            if (userRoles == null || !userRoles.Any())
            {
                throw new UnauthorizedAccessException("User does not have any roles assigned.");
            }

            var apiClientId = _configuration["Authorization:ApiSettings:ApiClientId"];
            if (string.IsNullOrWhiteSpace(apiClientId))
            {
                throw new InvalidOperationException("API client ID is missing from configuration.");
            }

            var scopeMappings = _configuration.GetSection("Authorization:ScopeMappings").Get<Dictionary<string, List<string>>>();
            if (scopeMappings == null)
            {
                throw new InvalidOperationException("ScopeMappings section is missing from configuration.");
            }

            // Map roles to scopes based on configuration, or use default scope if no roles match
            var apiScopes = userRoles.SelectMany(role => scopeMappings.ContainsKey(role) ? scopeMappings[role] : new List<string>())
                .Distinct()
                .Select(scope => $"api://{apiClientId}/{scope}")
                .ToArray();

            if (!apiScopes.Any())
            {
                var defaultScope = _configuration["ApiSettings:DefaultScope"];
                apiScopes = new[] { $"api://{apiClientId}/{defaultScope}" };
            }

            // Acquire the access token with the determined API scopes
            var apiToken = await _tokenAcquisition.GetAccessTokenForUserAsync(apiScopes, user: _httpContextAccessor.HttpContext?.User, authenticationScheme: authenticationScheme);

            return apiToken;
        }
    }
}
