using GovUK.Dfe.CoreLibs.Security.Configurations;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using GovUK.Dfe.CoreLibs.Security.Models;

namespace GovUK.Dfe.CoreLibs.Security.Authorization
{
    /// <inheritdoc />
    public class UserTokenService(
        IOptions<TokenSettings> tokenSettings,
        ILogger<UserTokenService> logger)
        : IUserTokenService
    {
        private readonly TokenSettings _tokenSettings = tokenSettings.Value;

        /// <inheritdoc />
        public Task<string> GetUserTokenAsync(ClaimsPrincipal user)
        {
            return GenerateJwtTokenAsync(user);
        }

        /// <inheritdoc />
        public async Task<Token> GetUserTokenModelAsync(ClaimsPrincipal user)
        {
            var jwt = await GenerateJwtTokenAsync(user);
            var expiresInSeconds = ComputeExpiresInSeconds(jwt);

            var tokenModel = new Token
            {
                AccessToken = jwt,
                TokenType = "Bearer",
                ExpiresIn = expiresInSeconds
            };

            return tokenModel;
        }

        private Task<string> GenerateJwtTokenAsync(ClaimsPrincipal user)
        {
            ArgumentNullException.ThrowIfNull(user);

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("User does not have a valid identifier.");

            var token = GenerateJwtTokenString(user);

            logger.LogInformation("Token generated for user: {UserId}", userId);

            return Task.FromResult(token);
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
