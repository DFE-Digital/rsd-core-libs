using DfE.CoreLibs.Security.Configurations;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;

namespace DfE.CoreLibs.Security.Authorization
{
    /// <inheritdoc />
    public class UserTokenService : IUserTokenService
    {
        private readonly TokenSettings _tokenSettings;
        private readonly IMemoryCache _cache;
        private readonly ILogger<UserTokenService> _logger;

        /// <summary>
        /// Defines the buffer time (in seconds) before the token's actual expiration
        /// to renew the token proactively.
        /// </summary>
        private const int CacheExpirationBufferSeconds = 30;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserTokenService"/> class.
        /// </summary>
        /// <param name="tokenSettings">Settings related to token generation, such as secret key, issuer, audience, and token lifetime.</param>
        /// <param name="cache">Memory cache used to store and retrieve cached tokens.</param>
        /// <param name="logger">Logger instance for logging informational and error messages.</param>
        public UserTokenService(IOptions<TokenSettings> tokenSettings, IMemoryCache cache, ILogger<UserTokenService> logger)
        {
            _tokenSettings = tokenSettings.Value;
            _cache = cache;
            _logger = logger;
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
            var claims = user.Claims
                .Select(c => new Claim(c.Type, c.Value))
                .ToList();

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_tokenSettings.SecretKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

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
