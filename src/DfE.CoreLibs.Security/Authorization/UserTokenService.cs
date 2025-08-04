using DfE.CoreLibs.Security.Configurations;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DfE.CoreLibs.Caching.Helpers;
using Microsoft.Extensions.Options;
using DfE.CoreLibs.Security.Models;

namespace DfE.CoreLibs.Security.Authorization
{
    /// <inheritdoc />
    public class UserTokenService(
        IOptions<TokenSettings> tokenSettings,
        IMemoryCache cache,
        ILogger<UserTokenService> logger)
        : IUserTokenService
    {
        private readonly TokenSettings _tokenSettings = tokenSettings.Value;

        /// <summary>
        /// Defines the buffer time (in seconds) before the token's actual expiration
        /// to renew the token proactively.
        /// </summary>
        private const int CacheExpirationBufferSeconds = 30;

        /// <inheritdoc />
        public Task<string> GetUserTokenAsync(ClaimsPrincipal user)
        {
            return GetOrCreateJwtTokenAsync(user);
        }

        /// <inheritdoc />
        public async Task<Token> GetUserTokenModelAsync(ClaimsPrincipal user)
        {
            var jwt = await GetOrCreateJwtTokenAsync(user);
            var expiresInSeconds = ComputeExpiresInSeconds(jwt);

            var tokenModel = new Token
            {
                AccessToken = jwt,
                TokenType = "Bearer",
                ExpiresIn = expiresInSeconds
            };

            return tokenModel;
        }

        private async Task<string> GetOrCreateJwtTokenAsync(ClaimsPrincipal user)
        {
            ArgumentNullException.ThrowIfNull(user);

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("User does not have a valid identifier.");

            var cacheKey = BuildCacheKey(userId, user);

            if (cache.TryGetValue(cacheKey, out string? cachedToken))
            {
                logger.LogInformation("Token retrieved from cache for user: {UserId} and cache key: {CacheKey}", userId, cacheKey);
                return cachedToken!;
            }

            var token = GenerateJwtTokenString(user);

            var expiration = DateTime.UtcNow
                .AddMinutes(_tokenSettings.TokenLifetimeMinutes)
                .Subtract(TimeSpan.FromSeconds(CacheExpirationBufferSeconds));

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiration);

            cache.Set(cacheKey, token, cacheEntryOptions);

            logger.LogInformation("Token generated and cached for user: {UserId}", userId);

            return token;
        }

        private static string BuildCacheKey(string userId, ClaimsPrincipal user)
        {
            var claimStrings = user.Claims
                .OrderBy(c => c.Type)
                .Select(c => $"{c.Type}:{c.Value}")
                .ToList();

            var hashed = CacheKeyHelper.GenerateHashedCacheKey(claimStrings);
            return $"UserToken_{userId}_{hashed}";
        }

        /// <summary>
        /// Generates a new JWT token string for the specified authenticated user.
        /// </summary>
        private string GenerateJwtTokenString(ClaimsPrincipal user)
        {
            var claims = user.Claims
                .Select(c => new Claim(c.Type, c.Value))
                .ToList();

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _tokenSettings.Issuer,
                audience: _tokenSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_tokenSettings.TokenLifetimeMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private int ComputeExpiresInSeconds(string jwtToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(jwtToken); // just reads, no validation
                var remaining = (int)Math.Max(0, (jwt.ValidTo - DateTime.UtcNow).TotalSeconds);

                // treat very small remaining as expired
                if (remaining < 5)
                    return 0;

                return remaining;
            }
            catch
            {
                // Can't parse: force refresh
                return 0;
            }
        }
    }
}
