using DfE.CoreLibs.Security.Authorization;
using DfE.CoreLibs.Security.Configurations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DfE.CoreLibs.Security.Tests.AuthorizationTests
{
    public class UserTokenServiceTests
    {
        private readonly IMemoryCache _memoryCacheMock;
        private readonly ILogger<UserTokenService> _loggerMock;
        private readonly IOptions<TokenSettings> _tokenSettingsMock;
        private readonly TokenSettings _tokenSettings;
        private readonly IConfiguration _configuration;
        private readonly ClaimsPrincipal _testUser;

        public UserTokenServiceTests()
        {
            _memoryCacheMock = Substitute.For<IMemoryCache>();
            _loggerMock = Substitute.For<ILogger<UserTokenService>>();

            // Load configuration from appsettings.json
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory) // Ensure the path to the test directory
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            // Configure TokenSettings from appsettings
            _tokenSettings = _configuration.GetSection("Authorization:TokenSettings").Get<TokenSettings>();
            _tokenSettingsMock = Substitute.For<IOptions<TokenSettings>>();
            _tokenSettingsMock.Value.Returns(_tokenSettings);

            // Setup test user
            _testUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth"));
        }

        [Fact]
        public async Task GetUserTokenAsync_ReturnsCachedToken_WhenTokenExists()
        {
            // Arrange
            var cacheKey = "UserToken_test-user-id";
            _memoryCacheMock.TryGetValue(cacheKey, out Arg.Any<object>()!)
                .Returns(call =>
                {
                    call[1] = "cached-token";
                    return true;
                });

            var service = new UserTokenService(_tokenSettingsMock, _memoryCacheMock, _loggerMock);

            // Act
            var token = await service.GetUserTokenAsync(_testUser);

            // Assert
            Assert.Equal("cached-token", token);
            _loggerMock.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString() == "Token retrieved from cache for user: test-user-id"),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
        }

        [Fact]
        public async Task GetUserTokenAsync_GeneratesAndCachesToken_WhenTokenNotInCache()
        {
            // Arrange
            var cacheKey = "UserToken_test-user-id";
            _memoryCacheMock.TryGetValue(cacheKey, out Arg.Any<object>()!)
                .Returns(false);

            var cacheEntryMock = Substitute.For<ICacheEntry>();
            _memoryCacheMock.CreateEntry(cacheKey).Returns(cacheEntryMock);

            var service = new UserTokenService(_tokenSettingsMock, _memoryCacheMock, _loggerMock);

            // Act
            var token = await service.GetUserTokenAsync(_testUser);

            // Assert
            Assert.NotNull(token);
            _memoryCacheMock.Received(1).CreateEntry(cacheKey);
            cacheEntryMock.Received(1).Value = token;
            _loggerMock.Received(0).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString() == "Token retrieved from cache for user: test-user-id"),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public async Task GetUserTokenAsync_ThrowsException_WhenUserIsNull()
        {
            // Arrange
            var service = new UserTokenService(_tokenSettingsMock, _memoryCacheMock, _loggerMock);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetUserTokenAsync(null));
            Assert.Equal("user", exception.ParamName);
        }

        [Fact]
        public async Task GetUserTokenAsync_ThrowsException_WhenUserIdIsInvalid()
        {
            // Arrange
            var invalidUser = new ClaimsPrincipal(new ClaimsIdentity()); // No claims
            var service = new UserTokenService(_tokenSettingsMock, _memoryCacheMock, _loggerMock);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUserTokenAsync(invalidUser));
            Assert.Equal("User does not have a valid identifier.", exception.Message);
        }

        [Fact]
        public void GenerateToken_CreatesValidJwtToken()
        {
            // Arrange
            var user = _testUser;
            var service = new UserTokenService(_tokenSettingsMock, _memoryCacheMock, _loggerMock);

            // Act
            var token = service.GetType()
                .GetMethod("GenerateToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(service, new object[] { user }) as string;

            // Assert
            Assert.NotNull(token);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            Assert.Equal(_tokenSettings.Issuer, jwtToken.Issuer);
            Assert.Equal(_tokenSettings.Audience, jwtToken.Audiences.First());
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Name && c.Value == "TestUser");
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        }
    }
}
