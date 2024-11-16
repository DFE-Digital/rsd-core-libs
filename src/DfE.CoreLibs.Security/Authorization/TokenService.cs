using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System.Security.Claims;
using DfE.CoreLibs.Security.Configurations;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace DfE.CoreLibs.Security.Authorization
{
    /// <inheritdoc />
    public class TokenService : ITokenService
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly TokenSettings _tokenSettings;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TokenService> _logger;

        /// <summary>
        /// Defines the buffer time (in seconds) before the token's actual expiration
        /// to renew the token proactively.
        /// </summary>
        private const int CacheExpirationBufferSeconds = 30;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenService"/> class.
        /// </summary>
        /// <param name="tokenAcquisition">The token acquisition service for acquiring tokens.</param>
        /// <param name="httpContextAccessor">Accessor for the current HTTP context, used to retrieve the user's claims.</param>
        /// <param name="configuration">Configuration used to retrieve role-to-scope mappings.</param>
        public TokenService(ITokenAcquisition tokenAcquisition, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, TokenSettings tokenSettings, IMemoryCache cache, ILogger<TokenService> logger)
        {
            _tokenAcquisition = tokenAcquisition;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _tokenSettings = tokenSettings;
            _cache = cache;
            _logger = logger;
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

        /// <inheritdoc />
        public Task<string> GetUserTokenAsync(ClaimsPrincipal user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            // Generate a unique cache key based on the user's unique identifier
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("User does not have a valid identifier.");

            var cacheKey = $"UserToken_{userId}";

            // Try to get the token from cache
            if (_cache.TryGetValue(cacheKey, out string? cachedToken))
            {
                _logger.LogInformation("Token retrieved from cache for user: {UserId}", userId);
                return Task.FromResult(cachedToken!);
            }

            // Generate a new token
            var token = GenerateToken(user);

            var expiration = DateTime.UtcNow.AddMinutes(_tokenSettings.TokenLifetimeMinutes)
                .Subtract(TimeSpan.FromSeconds(CacheExpirationBufferSeconds));

            // Set cache options
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiration);

            // Save the token in cache
            _cache.Set(cacheKey, token, cacheEntryOptions);

            return Task.FromResult(token);
        }

        /// <summary>
        /// Generates a new JWT token for the specified authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user for whom the token is to be generated.</param>
        /// <returns>The generated JWT token as a string.</returns>
        private string GenerateToken(ClaimsPrincipal user)
        {
            // Extract claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Identity?.Name ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty)
            };

            // Add role claims
            var roleClaims = user.Claims.Where(c => c.Type == ClaimTypes.Role);
            claims.AddRange(roleClaims);

            // Create the symmetric security key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenSettings.SecretKey));

            // Create signing credentials
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create the token
            var token = new JwtSecurityToken(
                issuer: _tokenSettings.Issuer,
                audience: _tokenSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_tokenSettings.TokenLifetimeMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
