using DfE.CoreLibs.Security.Authorization;
using DfE.CoreLibs.Security.Configurations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DfE.CoreLibs.Caching.Helpers;

namespace DfE.CoreLibs.Security.Tests.AuthorizationTests
{
    public class UserTokenServiceTests
    {
        private readonly IMemoryCache _memoryCacheMock;
        private readonly ILogger<UserTokenService> _loggerMock;
        private readonly IOptions<TokenSettings> _tokenSettingsMock;
        private readonly TokenSettings? _tokenSettings;
        private readonly ClaimsPrincipal _testUser;

        public UserTokenServiceTests()
        {
            _memoryCacheMock = Substitute.For<IMemoryCache>();
            _loggerMock = Substitute.For<ILogger<UserTokenService>>();

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            _tokenSettings = configuration.GetSection("Authorization:TokenSettings").Get<TokenSettings>();
            _tokenSettingsMock = Substitute.For<IOptions<TokenSettings>>();
            _tokenSettingsMock.Value.Returns(_tokenSettings!);

            // Setup test user
            _testUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth"));
        }

        private string ComputeExpectedCacheKey()
        {
            var claimStrings = _testUser.Claims
                .OrderBy(c => c.Type)
                .Select(c => $"{c.Type}:{c.Value}");
            var hash = CacheKeyHelper.GenerateHashedCacheKey(claimStrings);
            return $"UserToken_test-user-id_{hash}";
        }

        [Fact]
        public async Task GetUserTokenAsync_ReturnsCachedToken_WhenTokenExists()
        {
            // Arrange
            var expectedKey = ComputeExpectedCacheKey();
            _memoryCacheMock
                .TryGetValue(expectedKey, out Arg.Any<object>()!)
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
                Arg.Is<object>(o => o.ToString()!.Contains("Token retrieved from cache for user: test-user-id")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public async Task GetUserTokenAsync_GeneratesAndCachesToken_WhenTokenNotInCache()
        {
            // Arrange
            var expectedKey = ComputeExpectedCacheKey();
            _memoryCacheMock
                .TryGetValue(expectedKey, out Arg.Any<object>()!)
                .Returns(false);

            var cacheEntryMock = Substitute.For<ICacheEntry>();
            _memoryCacheMock.CreateEntry(expectedKey).Returns(cacheEntryMock);

            var service = new UserTokenService(_tokenSettingsMock, _memoryCacheMock, _loggerMock);

            // Act
            var token = await service.GetUserTokenAsync(_testUser);

            // Assert
            Assert.NotNull(token);
        }

        [Fact]
        public async Task GetUserTokenAsync_ThrowsException_WhenUserIsNull()
        {
            // Arrange
            var service = new UserTokenService(_tokenSettingsMock, _memoryCacheMock, _loggerMock);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetUserTokenAsync(null!));
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
            var service = new UserTokenService(_tokenSettingsMock, _memoryCacheMock, _loggerMock);

            // Act
            var token = service.GetType()
                .GetMethod("GenerateToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(service, new object[] { _testUser }) as string;

            // Assert
            Assert.NotNull(token);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            Assert.Equal(_tokenSettings!.Issuer, jwtToken.Issuer);
            Assert.Equal(_tokenSettings.Audience, jwtToken.Audiences.First());
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Name && c.Value == "TestUser");
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        }
    }
}
