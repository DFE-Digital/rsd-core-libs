using GovUK.Dfe.CoreLibs.Security.Authorization;
using GovUK.Dfe.CoreLibs.Security.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GovUK.Dfe.CoreLibs.Security.Tests.AuthorizationTests
{
    public class UserTokenServiceTests
    {
        private readonly ILogger<UserTokenService> _logger;
        private readonly IOptions<TokenSettings> _tokenSettingsOptions;
        private readonly TokenSettings _tokenSettings;
        private readonly ClaimsPrincipal _testUser;

        public UserTokenServiceTests()
        {
            _logger = Substitute.For<ILogger<UserTokenService>>();
            
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            _tokenSettings = configuration.GetSection("Authorization:TokenSettings").Get<TokenSettings>()
                             ?? new TokenSettings
                             {
                                 SecretKey = "super_secret_key_that_is_long_enough_for_hmacsha256",
                                 Issuer = "test-issuer",
                                 Audience = "test-audience",
                                 TokenLifetimeMinutes = 5
                             };
            _tokenSettingsOptions = Options.Create(_tokenSettings);

            _testUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth"));
        }

        private ClaimsPrincipal BuildTestUser()
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-123"),
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test"));
        }

        [Fact]
        public async Task GetUserTokenAsync_GeneratesNewToken_OnEachCall()
        {
            // Arrange
            var service = new UserTokenService(_tokenSettingsOptions, _logger);

            // Act
            var token1 = await service.GetUserTokenAsync(_testUser);
            // Add small delay to ensure different timestamps in JWT
            await Task.Delay(1100); // Ensure at least 1 second difference for JWT timestamp
            var token2 = await service.GetUserTokenAsync(_testUser);

            // Assert
            Assert.NotNull(token1);
            Assert.NotNull(token2);
            // Tokens should be different since a new one is generated each time with different timestamps
            Assert.NotEqual(token1, token2);
            _logger.Received(2).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Token generated for user: test-user-id")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public async Task GetUserTokenAsync_ThrowsException_WhenUserIsNull()
        {
            // Arrange
            var service = new UserTokenService(_tokenSettingsOptions, _logger);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetUserTokenAsync(null!));
            Assert.Equal("user", exception.ParamName);
        }

        [Fact]
        public async Task GetUserTokenAsync_ThrowsException_WhenUserIdIsInvalid()
        {
            // Arrange
            var invalidUser = new ClaimsPrincipal(new ClaimsIdentity()); // No claims
            var service = new UserTokenService(_tokenSettingsOptions, _logger);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUserTokenAsync(invalidUser));
            Assert.Equal("User does not have a valid identifier.", exception.Message);
        }

        [Fact]
        public async Task GetUserTokenAsync_CreatesValidJwtToken()
        {
            // Arrange
            var service = new UserTokenService(_tokenSettingsOptions, _logger);

            // Act
            var token = await service.GetUserTokenAsync(_testUser);

            // Assert
            Assert.NotNull(token);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token!);
            Assert.Equal(_tokenSettings.Issuer, jwtToken.Issuer);
            Assert.Contains(_tokenSettings.Audience, jwtToken.Audiences);
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Name && c.Value == "TestUser");
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        }

        [Fact]
        public async Task GetUserTokenModelAsync_ReturnsTokenModel_WithValidJwtAndExpiresIn()
        {
            // Arrange
            var service = new UserTokenService(_tokenSettingsOptions, _logger);
            var user = BuildTestUser();

            // Act
            var tokenModel = await service.GetUserTokenModelAsync(user);

            // Assert
            Assert.NotNull(tokenModel);
            Assert.False(string.IsNullOrWhiteSpace(tokenModel.AccessToken));
            Assert.Equal("Bearer", tokenModel.TokenType);

            // ExpiresIn should reflect remaining lifetime: positive and not more than configured
            var configuredSeconds = _tokenSettings.TokenLifetimeMinutes * 60;
            Assert.InRange(tokenModel.ExpiresIn, 0, configuredSeconds);
            // Should be reasonably close to full lifetime (allow some slack)
            Assert.True(tokenModel.ExpiresIn > configuredSeconds - 60, $"ExpiresIn too small: {tokenModel.ExpiresIn}");

            // Inspect the embedded JWT for correctness
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(tokenModel.AccessToken);
            Assert.Equal(_tokenSettings.Issuer, jwt.Issuer);
            Assert.Contains(_tokenSettings.Audience, jwt.Audiences);
            Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Name && c.Value == "TestUser");
            Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
            Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user-123");
        }

        [Fact]
        public async Task GetUserTokenModelAsync_GeneratesNewToken_OnEachCall()
        {
            // Arrange
            var service = new UserTokenService(_tokenSettingsOptions, _logger);
            var user = BuildTestUser();

            // Act - multiple calls should generate different tokens
            var first = await service.GetUserTokenModelAsync(user);
            // Add small delay to ensure different timestamps in JWT
            await Task.Delay(1100); // Ensure at least 1 second difference for JWT timestamp
            var second = await service.GetUserTokenModelAsync(user);

            // Assert
            Assert.NotEqual(first.AccessToken, second.AccessToken); // different tokens generated
            Assert.Equal("Bearer", first.TokenType);
            Assert.Equal("Bearer", second.TokenType);

            // Both tokens should have valid expiration times
            var configuredSeconds = _tokenSettings.TokenLifetimeMinutes * 60;
            Assert.InRange(first.ExpiresIn, 0, configuredSeconds);
            Assert.InRange(second.ExpiresIn, 0, configuredSeconds);
        }

        [Fact]
        public async Task GetUserTokenModelAsync_NoIdentifier_ThrowsInvalidOperationException()
        {
            // Arrange
            var service = new UserTokenService(_tokenSettingsOptions, _logger);
            // Build a principal without NameIdentifier or Name
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("some", "claim") }, "Test"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUserTokenModelAsync(user));
        }

        [Fact]
        public async Task GetUserTokenModelAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var service = new UserTokenService(_tokenSettingsOptions, _logger);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetUserTokenModelAsync(null!));
            Assert.Equal("user", ex.ParamName);
        }

        [Fact]
        public async Task GetUserTokenModelAsync_ReturnsDifferentTokens_ForDifferentUsers()
        {
            // Arrange
            var service = new UserTokenService(_tokenSettingsOptions, _logger);
            var userA = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-a"),
                new Claim(ClaimTypes.Name, "UserA"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test"));
            var userB = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-b"),
                new Claim(ClaimTypes.Name, "UserB"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test"));

            // Act
            var tokenA = await service.GetUserTokenModelAsync(userA);
            var tokenB = await service.GetUserTokenModelAsync(userB);

            // Assert
            Assert.NotEqual(tokenA.AccessToken, tokenB.AccessToken);
        }
    }
}
