using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using System.Security.Claims;
using DfE.CoreLibs.Security.Configurations;

namespace DfE.CoreLibs.Security.Authorization
{

    /// <summary>
    /// Provides functionality for acquiring API On-Behalf-Of (OBO) tokens for authenticated users with caching.
    /// </summary>
    public class ApiOboTokenService(
        ITokenAcquisition tokenAcquisition,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IMemoryCache memoryCache,
        IOptions<TokenSettings> tokenSettingsOptions)
        : IApiOboTokenService
    {
        private readonly TokenSettings _tokenSettings = tokenSettingsOptions.Value;

        /// <inheritdoc />
        public async Task<string> GetApiOboTokenAsync(string? authenticationScheme = null)
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("User ID is missing.");
            }

            // Retrieve user roles
            var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
            if (userRoles == null || !userRoles.Any())
            {
                throw new UnauthorizedAccessException("User does not have any roles assigned.");
            }

            // Retrieve API client ID from configuration
            var apiClientId = configuration["Authorization:ApiSettings:ApiClientId"];
            if (string.IsNullOrWhiteSpace(apiClientId))
            {
                throw new InvalidOperationException("API client ID is missing from configuration.");
            }

            // Retrieve scope mappings from configuration
            var scopeMappings = configuration.GetSection("Authorization:ScopeMappings").Get<Dictionary<string, List<string>>>();
            if (scopeMappings == null)
            {
                throw new InvalidOperationException("ScopeMappings section is missing from configuration.");
            }

            // Map roles to scopes based on configuration
            var apiScopes = userRoles
                .SelectMany(role => scopeMappings.TryGetValue(role, out var mapping) ? mapping : new List<string>())
                .Distinct()
                .ToList();

            if (!apiScopes.Any())
            {
                var defaultScope = configuration["Authorization:ApiSettings:DefaultScope"];
                apiScopes = [defaultScope!];
            }

            // Sort scopes to ensure consistent cache key generation
            apiScopes.Sort(StringComparer.OrdinalIgnoreCase);
            var scopesString = string.Join(",", apiScopes);

            // Generate a unique cache key based on user ID and scopes
            var cacheKey = $"ApiOboToken_{userId}_{scopesString}";

            if (memoryCache.TryGetValue<string>(cacheKey, out var cachedToken))
            {
                return cachedToken!;
            }

            // Acquire a new token
            var formattedScopes = apiScopes.Select(scope => $"api://{apiClientId}/{scope}").ToArray();

            var apiToken = await tokenAcquisition.GetAccessTokenForUserAsync(
                formattedScopes,
                user: user,
                authenticationScheme: authenticationScheme);

            // Calculate absolute expiration time: Now + Expiration - Buffer
            var absoluteExpiration = DateTimeOffset.UtcNow
                .AddMinutes(_tokenSettings.TokenLifetimeMinutes)
                .Subtract(TimeSpan.FromSeconds(_tokenSettings.BufferInSeconds));

            // Cache the token with absolute expiration
            memoryCache.Set(cacheKey, apiToken, absoluteExpiration);

            return apiToken;
        }
    }
}