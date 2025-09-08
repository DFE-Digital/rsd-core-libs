using GovUK.Dfe.CoreLibs.Security.Authorization;
using GovUK.Dfe.CoreLibs.Security.Configurations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Moq;
using NSubstitute;
using System.Security.Claims;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace GovUK.Dfe.CoreLibs.Security.Tests.AuthorizationTests
{
    public class ApiOboTokenServiceTests
    {
        private readonly ITokenAcquisition _tokenAcquisitionMock;
        private readonly IHttpContextAccessor _httpContextAccessorMock;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCacheMock;
        private readonly IOptions<TokenSettings> _tokenSettingsMock;
        private readonly ClaimsPrincipal _testUser;

        public ApiOboTokenServiceTests()
        {
            _tokenAcquisitionMock = Substitute.For<ITokenAcquisition>();
            _httpContextAccessorMock = Substitute.For<IHttpContextAccessor>();
            _memoryCacheMock = Substitute.For<IMemoryCache>();

            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory) // Ensure the path to the test directory
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            // Configure TokenSettings from appsettings
            var tokenSettings = _configuration.GetSection("Authorization:TokenSettings").Get<TokenSettings>();
            _tokenSettingsMock = Substitute.For<IOptions<TokenSettings>>();
            _tokenSettingsMock.Value.Returns(tokenSettings);

            // Setup test user
            _testUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Role, "Admin"),
        }, "TestAuth"));
        }

        [Fact]
        public async Task GetApiOboTokenAsync_UserNotAuthenticated_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _httpContextAccessorMock.HttpContext.Returns((HttpContext)null!);

            var service = new ApiOboTokenService(
                _tokenAcquisitionMock,
                _httpContextAccessorMock,
                _configuration,
                _memoryCacheMock,
                _tokenSettingsMock);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GetApiOboTokenAsync());
        }

        [Fact]
        public async Task GetApiOboTokenAsync_UserHasNoRoles_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _httpContextAccessorMock.HttpContext.Returns(new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id")
            }, "TestAuth"))
            });

            var service = new ApiOboTokenService(
                _tokenAcquisitionMock,
                _httpContextAccessorMock,
                _configuration,
                _memoryCacheMock,
                _tokenSettingsMock);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GetApiOboTokenAsync());
        }

        [Fact]
        public async Task GetApiOboTokenAsync_ApiClientIdMissing_ThrowsInvalidOperationException()
        {
            // Arrange
            IConfiguration _configurationMock = Substitute.For<IConfiguration>();

            _httpContextAccessorMock.HttpContext.Returns(new DefaultHttpContext { User = _testUser });
            _configurationMock["Authorization:ApiSettings:ApiClientId"].Returns((string)null!);

            var service = new ApiOboTokenService(
                _tokenAcquisitionMock,
                _httpContextAccessorMock,
                _configurationMock,
                _memoryCacheMock,
                _tokenSettingsMock);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GetApiOboTokenAsync());
        }

        [Fact]
        public async Task GetApiOboTokenAsync_TokenCached_ReturnsCachedToken()
        {
            _httpContextAccessorMock.HttpContext.Returns(new DefaultHttpContext { User = _testUser });

            var formattedScopes = new[] { "api://test-api-client-id/Scope1" };

            var cacheKey = "ApiOboToken_test-user-id,Scope1";

            // Simulate retrieving a cached token
            _memoryCacheMock.TryGetValue(cacheKey, out Arg.Any<object>()!)
                .Returns(call =>
                {
                    call[1] = "cached-token";
                    return true;
                });

            _tokenAcquisitionMock
                .GetAccessTokenForUserAsync(
                    Arg.Is<IEnumerable<string>>(scopes => scopes.SequenceEqual(formattedScopes)),
                    Arg.Is<string>(scheme => scheme == null),
                    Arg.Is<string>(_ => true),
                    Arg.Is<string>(_ => true),
                    Arg.Is<ClaimsPrincipal>(user => user == _testUser),
                    Arg.Is<TokenAcquisitionOptions>(_ => true))
                .Returns("mock-token");

            var service = new ApiOboTokenService(
                _tokenAcquisitionMock,
                _httpContextAccessorMock,
                _configuration,
                _memoryCacheMock,
                _tokenSettingsMock);

            // Act
            var token = await service.GetApiOboTokenAsync();

            // Assert
            Assert.Equal("mock-token", token);
        }


        [Fact]
        public async Task GetApiOboTokenAsync_AcquiresNewToken_AndCachesIt_WithInMemoryConfig()
        {
            // Arrange
            var memoryCacheMock = new Mock<IMemoryCache>();
            var cacheKey = "ApiOboToken_test-user-id_Scope1";

            // Setup TryGetValue to simulate a cache miss
            object cachedValue = null!;
            memoryCacheMock
                .Setup(mc => mc.TryGetValue(It.Is<string>(key => key == cacheKey), out cachedValue!))
                .Returns(false);

            // Mock CreateEntry for caching behavior
            var cacheEntryMock = new Mock<ICacheEntry>();
            memoryCacheMock
                .Setup(mc => mc.CreateEntry(It.Is<string>(key => key == cacheKey)))
                .Returns(cacheEntryMock.Object);

            var tokenAcquisitionMock = new Mock<ITokenAcquisition>();
            tokenAcquisitionMock
                .Setup(ta => ta.GetAccessTokenForUserAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<TokenAcquisitionOptions>()))
                .ReturnsAsync("mock-token");

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(hca => hca.HttpContext)
                .Returns(new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Role, "Admin")
                }))
                });

            // Use In-Memory Configuration
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
            { "Authorization:ApiSettings:ApiClientId", "test-api-client-id" },
            { "Authorization:ScopeMappings:Admin:0", "Scope1" }
                }!)
                .Build();

            var tokenSettingsMock = new Mock<IOptions<TokenSettings>>();
            tokenSettingsMock
                .Setup(ts => ts.Value)
                .Returns(new TokenSettings
                {
                    TokenLifetimeMinutes = 10,
                    BufferInSeconds = 60
                });

            var service = new ApiOboTokenService(
                tokenAcquisitionMock.Object,
                httpContextAccessorMock.Object,
                configuration,
                memoryCacheMock.Object,
                tokenSettingsMock.Object);

            // Act
            var token = await service.GetApiOboTokenAsync();

            // Assert
            Assert.Equal("mock-token", token);

            // Verify CreateEntry was called with correct arguments
            memoryCacheMock.Verify(mc => mc.CreateEntry(It.Is<string>(key => key == cacheKey)), Times.Once);

            // Verify the cache entry value
            cacheEntryMock.VerifySet(ce => ce.Value = "mock-token");
        }

        [Fact]
        public async Task GetApiOboTokenAsync_ThrowsException_WhenUserIdIsMissing()
        {
            // Arrange
            _httpContextAccessorMock.HttpContext
                .Returns(new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                });

            var service = new ApiOboTokenService(
                _tokenAcquisitionMock,
                _httpContextAccessorMock,
                _configuration,
                _memoryCacheMock,
                _tokenSettingsMock);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetApiOboTokenAsync());
            Assert.Equal("User ID is missing.", exception.Message);
        }

        [Fact]
        public async Task GetApiOboTokenAsync_ThrowsException_WhenScopeMappingsMissing()
        {
            // Arrange
            var configurationMock = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Authorization:ApiSettings:ApiClientId", "test-api-client-id" }
                }!)
                .Build();

            var service = new ApiOboTokenService(
                _tokenAcquisitionMock,
                _httpContextAccessorMock,
                configurationMock,
                _memoryCacheMock,
                _tokenSettingsMock);

            _httpContextAccessorMock.HttpContext.Returns(new DefaultHttpContext
            {
                User = _testUser
            });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetApiOboTokenAsync());
            Assert.Equal("ScopeMappings section is missing from configuration.", exception.Message);
        }

        [Fact]
        public async Task GetApiOboTokenAsync_UsesDefaultScope_WhenNoApiScopes()
        {
            // Arrange
            var configurationMock = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Authorization:ApiSettings:ApiClientId", "test-api-client-id" },
                    { "Authorization:ApiSettings:DefaultScope", "default-scope" },
                    { "Authorization:ScopeMappings:Admin:0", "Scope1" }
                }!)
                .Build();

            var service = new ApiOboTokenService(
                _tokenAcquisitionMock,
                _httpContextAccessorMock,
                configurationMock,
                _memoryCacheMock,
                _tokenSettingsMock);

            _httpContextAccessorMock.HttpContext.Returns(new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                    new Claim(ClaimTypes.Role, "UnknownRole") // Role not mapped
                }))
            });

            var defaultScopes = new[] { "api://test-api-client-id/default-scope" };

            _tokenAcquisitionMock
                .GetAccessTokenForUserAsync(
                    Arg.Is<IEnumerable<string>>(scopes => scopes.SequenceEqual(defaultScopes)),
                    Arg.Is<string>(scheme => scheme == null),
                    Arg.Is<string>(_ => true),
                    Arg.Is<string>(_ => true),
                    Arg.Is<ClaimsPrincipal>(user => true),
                    Arg.Is<TokenAcquisitionOptions>(_ => true))
                .Returns("mock-token");

            // Act
            var token = await service.GetApiOboTokenAsync();

            // Assert
            Assert.Equal("mock-token", token);
            await _tokenAcquisitionMock.Received(1).GetAccessTokenForUserAsync(
                Arg.Is<IEnumerable<string>>(scopes => scopes.SequenceEqual(defaultScopes)),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<TokenAcquisitionOptions>());
        }


        [Fact]
        public async Task GetApiOboTokenAsync_AcquiresNewToken_AndCachesIt()
        {
            // Arrange
            _httpContextAccessorMock.HttpContext.Returns(new DefaultHttpContext { User = _testUser });

            var formattedScopes = new[] { "api://test-api-client-id/Scope1" };

            // Simulate no cached token
            _memoryCacheMock.TryGetValue(Arg.Any<string>(), out Arg.Any<object>()!)
                .Returns(callInfo =>
                {
                    callInfo[1] = null;
                    return false;
                });

            // Simulate token acquisition
            _tokenAcquisitionMock
                .GetAccessTokenForUserAsync(
                    Arg.Is<IEnumerable<string>>(scopes => scopes.SequenceEqual(formattedScopes)),
                    Arg.Is<string>(scheme => scheme == null),
                    Arg.Is<string>(_ => true),
                    Arg.Is<string>(_ => true),
                    Arg.Is<ClaimsPrincipal>(user => user == _testUser),
                    Arg.Is<TokenAcquisitionOptions>(_ => true))
                .Returns("mock-token");


            var service = new ApiOboTokenService(
                _tokenAcquisitionMock,
                _httpContextAccessorMock,
                _configuration,
                _memoryCacheMock,
                _tokenSettingsMock);

            // Act
            var token = await service.GetApiOboTokenAsync();

            // Assert the token is returned correctly
            Assert.Equal("mock-token", token);
        }



        [Fact]
        public async Task GetApiOboTokenAsync_ReturnsCachedToken_WhenTokenExistsInCache()
        {
            // Arrange
            _httpContextAccessorMock.HttpContext.Returns(new DefaultHttpContext
            {
                User = _testUser
            });

            var cacheKey = "ApiOboToken_test-user-id_Scope1";

            _memoryCacheMock.TryGetValue(Arg.Any<string>(), out Arg.Any<object>()!)
                .Returns(callInfo =>
                {
                    // Access the cache key
                    var key = callInfo.Arg<string>();

                    // Simulate returning a cached value for a specific key
                    if (key == cacheKey)
                    {
                        callInfo[1] = "cached-token"; 
                        return true; 
                    }
                    callInfo[1] = null;         
                    return false;
                });

            var service = new ApiOboTokenService(
                _tokenAcquisitionMock,
                _httpContextAccessorMock,
                _configuration,
                _memoryCacheMock,
                _tokenSettingsMock);

            // Act
            var token = await service.GetApiOboTokenAsync();

            // Assert
            Assert.Equal("cached-token", token);
        }
    }
}
