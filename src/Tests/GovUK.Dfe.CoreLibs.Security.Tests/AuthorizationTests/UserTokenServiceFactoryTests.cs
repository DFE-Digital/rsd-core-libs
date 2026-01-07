using GovUK.Dfe.CoreLibs.Security.Authorization;
using GovUK.Dfe.CoreLibs.Security.Configurations;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GovUK.Dfe.CoreLibs.Security.Tests.AuthorizationTests
{
    public class UserTokenServiceFactoryTests
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOptionsMonitor<TokenSettings> _tokenSettingsMonitor;

        public UserTokenServiceFactoryTests()
        {
            _loggerFactory = Substitute.For<ILoggerFactory>();
            _tokenSettingsMonitor = Substitute.For<IOptionsMonitor<TokenSettings>>();

            // Setup logger factory to return a real logger
            var logger = Substitute.For<ILogger<UserTokenService>>();
            _loggerFactory.CreateLogger<UserTokenService>().Returns(logger);
        }

        [Fact]
        public void GetService_ShouldReturnUserTokenService_WithNamedConfiguration()
        {
            // Arrange
            var primarySettings = new TokenSettings
            {
                SecretKey = "primary_secret_key_that_is_long_enough_for_hmacsha256",
                Issuer = "primary-issuer",
                Audience = "primary-audience",
                TokenLifetimeMinutes = 60
            };

            _tokenSettingsMonitor.Get("Primary").Returns(primarySettings);

            var factory = new UserTokenServiceFactory(_tokenSettingsMonitor, _loggerFactory);

            // Act
            var service = factory.GetService("Primary");

            // Assert
            Assert.NotNull(service);
            Assert.IsAssignableFrom<IUserTokenService>(service);
            _tokenSettingsMonitor.Received(1).Get("Primary");
        }

        [Fact]
        public void GetService_ShouldReturnDifferentInstances_ForDifferentConfigurations()
        {
            // Arrange
            var primarySettings = new TokenSettings
            {
                SecretKey = "primary_secret_key_that_is_long_enough_for_hmacsha256",
                Issuer = "primary-issuer",
                Audience = "primary-audience",
                TokenLifetimeMinutes = 60
            };

            var secondarySettings = new TokenSettings
            {
                SecretKey = "secondary_secret_key_that_is_long_enough_for_hmacsha256",
                Issuer = "secondary-issuer",
                Audience = "secondary-audience",
                TokenLifetimeMinutes = 30
            };

            _tokenSettingsMonitor.Get("Primary").Returns(primarySettings);
            _tokenSettingsMonitor.Get("Secondary").Returns(secondarySettings);

            var factory = new UserTokenServiceFactory(_tokenSettingsMonitor, _loggerFactory);

            // Act
            var primaryService = factory.GetService("Primary");
            var secondaryService = factory.GetService("Secondary");

            // Assert
            Assert.NotNull(primaryService);
            Assert.NotNull(secondaryService);
            Assert.NotSame(primaryService, secondaryService);
            _tokenSettingsMonitor.Received(1).Get("Primary");
            _tokenSettingsMonitor.Received(1).Get("Secondary");
        }

        [Fact]
        public async Task GetService_ShouldGenerateTokens_WithCorrectConfiguration()
        {
            // Arrange
            var primarySettings = new TokenSettings
            {
                SecretKey = "primary_secret_key_that_is_long_enough_for_hmacsha256",
                Issuer = "primary-issuer",
                Audience = "primary-audience",
                TokenLifetimeMinutes = 60
            };

            var secondarySettings = new TokenSettings
            {
                SecretKey = "secondary_secret_key_that_is_long_enough_for_hmacsha256",
                Issuer = "secondary-issuer",
                Audience = "secondary-audience",
                TokenLifetimeMinutes = 30
            };

            _tokenSettingsMonitor.Get("Primary").Returns(primarySettings);
            _tokenSettingsMonitor.Get("Secondary").Returns(secondarySettings);

            var factory = new UserTokenServiceFactory(_tokenSettingsMonitor, _loggerFactory);
            var primaryService = factory.GetService("Primary");
            var secondaryService = factory.GetService("Secondary");

            var testUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth"));

            // Act
            var primaryToken = await primaryService.GetUserTokenAsync(testUser);
            var secondaryToken = await secondaryService.GetUserTokenAsync(testUser);

            // Assert
            Assert.NotNull(primaryToken);
            Assert.NotNull(secondaryToken);
            Assert.NotEqual(primaryToken, secondaryToken);

            // Verify primary token has correct issuer and audience
            var handler = new JwtSecurityTokenHandler();
            var primaryJwt = handler.ReadJwtToken(primaryToken);
            Assert.Equal("primary-issuer", primaryJwt.Issuer);
            Assert.Contains("primary-audience", primaryJwt.Audiences);

            // Verify secondary token has correct issuer and audience
            var secondaryJwt = handler.ReadJwtToken(secondaryToken);
            Assert.Equal("secondary-issuer", secondaryJwt.Issuer);
            Assert.Contains("secondary-audience", secondaryJwt.Audiences);
        }

        [Fact]
        public async Task GetService_ShouldGenerateTokenModels_WithCorrectLifetimes()
        {
            // Arrange
            var shortLivedSettings = new TokenSettings
            {
                SecretKey = "short_lived_secret_key_that_is_long_enough_for_hmacsha256",
                Issuer = "test-issuer",
                Audience = "test-audience",
                TokenLifetimeMinutes = 5 // 5 minutes
            };

            var longLivedSettings = new TokenSettings
            {
                SecretKey = "long_lived_secret_key_that_is_long_enough_for_hmacsha256",
                Issuer = "test-issuer",
                Audience = "test-audience",
                TokenLifetimeMinutes = 120 // 2 hours
            };

            _tokenSettingsMonitor.Get("ShortLived").Returns(shortLivedSettings);
            _tokenSettingsMonitor.Get("LongLived").Returns(longLivedSettings);

            var factory = new UserTokenServiceFactory(_tokenSettingsMonitor, _loggerFactory);
            var shortLivedService = factory.GetService("ShortLived");
            var longLivedService = factory.GetService("LongLived");

            var testUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "TestUser")
            }, "TestAuth"));

            // Act
            var shortLivedToken = await shortLivedService.GetUserTokenModelAsync(testUser);
            var longLivedToken = await longLivedService.GetUserTokenModelAsync(testUser);

            // Assert
            Assert.NotNull(shortLivedToken);
            Assert.NotNull(longLivedToken);
            Assert.Equal("Bearer", shortLivedToken.TokenType);
            Assert.Equal("Bearer", longLivedToken.TokenType);

            // Verify the expiration times are approximately correct
            var shortLivedExpectedSeconds = 5 * 60; // 5 minutes
            var longLivedExpectedSeconds = 120 * 60; // 2 hours

            Assert.InRange(shortLivedToken.ExpiresIn, shortLivedExpectedSeconds - 60, shortLivedExpectedSeconds);
            Assert.InRange(longLivedToken.ExpiresIn, longLivedExpectedSeconds - 60, longLivedExpectedSeconds);
            Assert.True(longLivedToken.ExpiresIn > shortLivedToken.ExpiresIn);
        }

        [Fact]
        public void GetService_ShouldCallOptionsMonitor_WithCorrectName()
        {
            // Arrange
            var settings = new TokenSettings
            {
                SecretKey = "test_secret_key_that_is_long_enough_for_hmacsha256",
                Issuer = "test-issuer",
                Audience = "test-audience",
                TokenLifetimeMinutes = 60
            };

            _tokenSettingsMonitor.Get("CustomConfig").Returns(settings);

            var factory = new UserTokenServiceFactory(_tokenSettingsMonitor, _loggerFactory);

            // Act
            var service = factory.GetService("CustomConfig");

            // Assert
            Assert.NotNull(service);
            _tokenSettingsMonitor.Received(1).Get("CustomConfig");
            _loggerFactory.Received(1).CreateLogger<UserTokenService>();
        }

        [Fact]
        public void GetService_ShouldCreateNewInstance_OnEachCall()
        {
            // Arrange
            var settings = new TokenSettings
            {
                SecretKey = "test_secret_key_that_is_long_enough_for_hmacsha256",
                Issuer = "test-issuer",
                Audience = "test-audience",
                TokenLifetimeMinutes = 60
            };

            _tokenSettingsMonitor.Get("Test").Returns(settings);

            var factory = new UserTokenServiceFactory(_tokenSettingsMonitor, _loggerFactory);

            // Act
            var service1 = factory.GetService("Test");
            var service2 = factory.GetService("Test");

            // Assert
            Assert.NotNull(service1);
            Assert.NotNull(service2);
            // Each call to GetService should create a new instance
            Assert.NotSame(service1, service2);
            _tokenSettingsMonitor.Received(2).Get("Test");
            _loggerFactory.Received(2).CreateLogger<UserTokenService>();
        }

        [Fact]
        public async Task MultipleServices_ShouldWorkIndependently()
        {
            // Arrange
            var config1 = new TokenSettings
            {
                SecretKey = "config1_secret_key_that_is_long_enough_for_hmacsha256",
                Issuer = "issuer1",
                Audience = "audience1",
                TokenLifetimeMinutes = 30
            };

            var config2 = new TokenSettings
            {
                SecretKey = "config2_secret_key_that_is_long_enough_for_hmacsha256",
                Issuer = "issuer2",
                Audience = "audience2",
                TokenLifetimeMinutes = 60
            };

            var config3 = new TokenSettings
            {
                SecretKey = "config3_secret_key_that_is_long_enough_for_hmacsha256",
                Issuer = "issuer3",
                Audience = "audience3",
                TokenLifetimeMinutes = 90
            };

            _tokenSettingsMonitor.Get("Config1").Returns(config1);
            _tokenSettingsMonitor.Get("Config2").Returns(config2);
            _tokenSettingsMonitor.Get("Config3").Returns(config3);

            var factory = new UserTokenServiceFactory(_tokenSettingsMonitor, _loggerFactory);
            var service1 = factory.GetService("Config1");
            var service2 = factory.GetService("Config2");
            var service3 = factory.GetService("Config3");

            var testUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user"),
                new Claim(ClaimTypes.Name, "TestUser")
            }, "TestAuth"));

            // Act
            var token1 = await service1.GetUserTokenAsync(testUser);
            var token2 = await service2.GetUserTokenAsync(testUser);
            var token3 = await service3.GetUserTokenAsync(testUser);

            // Assert - all tokens should be different and have correct issuers/audiences
            var handler = new JwtSecurityTokenHandler();
            
            var jwt1 = handler.ReadJwtToken(token1);
            Assert.Equal("issuer1", jwt1.Issuer);
            Assert.Contains("audience1", jwt1.Audiences);

            var jwt2 = handler.ReadJwtToken(token2);
            Assert.Equal("issuer2", jwt2.Issuer);
            Assert.Contains("audience2", jwt2.Audiences);

            var jwt3 = handler.ReadJwtToken(token3);
            Assert.Equal("issuer3", jwt3.Issuer);
            Assert.Contains("audience3", jwt3.Audiences);

            // All tokens should be different
            Assert.NotEqual(token1, token2);
            Assert.NotEqual(token2, token3);
            Assert.NotEqual(token1, token3);
        }
    }
}

