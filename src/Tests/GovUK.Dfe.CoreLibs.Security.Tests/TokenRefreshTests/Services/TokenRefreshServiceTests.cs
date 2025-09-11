using GovUK.Dfe.CoreLibs.Security.Models;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Configuration;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Exceptions;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Interfaces;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace GovUK.Dfe.CoreLibs.Security.Tests.TokenRefreshTests.Services
{
    public class TokenRefreshServiceTests
    {
        private readonly Mock<ITokenRefreshProvider> _tokenRefreshProviderMock;
        private readonly Mock<ITokenIntrospectionService> _tokenIntrospectionServiceMock;
        private readonly Mock<ILogger<TokenRefreshService>> _loggerMock;
        private readonly TokenRefreshOptions _options;
        private readonly TokenRefreshService _service;

        public TokenRefreshServiceTests()
        {
            _tokenRefreshProviderMock = new Mock<ITokenRefreshProvider>();
            _tokenIntrospectionServiceMock = new Mock<ITokenIntrospectionService>();
            _loggerMock = new Mock<ILogger<TokenRefreshService>>();
            _options = new TokenRefreshOptions
            {
                TokenEndpoint = "https://example.com/token",
                IntrospectionEndpoint = "https://example.com/introspect",
                ClientId = "test_client",
                ClientSecret = "test_secret",
                RefreshBufferMinutes = 5
            };
            
            var optionsMock = new Mock<IOptions<TokenRefreshOptions>>();
            optionsMock.Setup(x => x.Value).Returns(_options);

            _service = new TokenRefreshService(
                _tokenRefreshProviderMock.Object, 
                _tokenIntrospectionServiceMock.Object, 
                optionsMock.Object, 
                _loggerMock.Object);
        }

        [Fact]
        public void Constructor_WithNullTokenRefreshProvider_ThrowsArgumentNullException()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<TokenRefreshOptions>>();
            optionsMock.Setup(x => x.Value).Returns(_options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TokenRefreshService(null!, _tokenIntrospectionServiceMock.Object, optionsMock.Object, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullTokenIntrospectionService_ThrowsArgumentNullException()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<TokenRefreshOptions>>();
            optionsMock.Setup(x => x.Value).Returns(_options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TokenRefreshService(_tokenRefreshProviderMock.Object, null!, optionsMock.Object, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TokenRefreshService(_tokenRefreshProviderMock.Object, _tokenIntrospectionServiceMock.Object, null!, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<TokenRefreshOptions>>();
            optionsMock.Setup(x => x.Value).Returns(_options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TokenRefreshService(_tokenRefreshProviderMock.Object, _tokenIntrospectionServiceMock.Object, optionsMock.Object, null!));
        }

        [Fact]
        public async Task RefreshTokenAsync_WithNullRefreshToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.RefreshTokenAsync(null!));
        }

        [Fact]
        public async Task RefreshTokenAsync_WithEmptyRefreshToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.RefreshTokenAsync(""));
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidRefreshToken_ReturnsSuccessfulResponse()
        {
            // Arrange
            const string refreshToken = "test_refresh_token";
            var expectedToken = new Token
            {
                AccessToken = "new_access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                RefreshToken = "new_refresh_token"
            };
            var expectedResponse = TokenRefreshResponse.Success(expectedToken, true);

            _tokenRefreshProviderMock
                .Setup(x => x.RefreshTokenAsync(It.IsAny<TokenRefreshRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.RefreshTokenAsync(refreshToken);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedToken, result.Token);
            Assert.True(result.RefreshTokenRotated);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithProviderFailure_ReturnsFailureResponse()
        {
            // Arrange
            const string refreshToken = "test_refresh_token";
            const string errorMessage = "Invalid refresh token";
            const string errorCode = "invalid_grant";
            var expectedResponse = TokenRefreshResponse.Failure(errorMessage, errorCode);

            _tokenRefreshProviderMock
                .Setup(x => x.RefreshTokenAsync(It.IsAny<TokenRefreshRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.RefreshTokenAsync(refreshToken);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithUnexpectedException_ThrowsTokenRefreshException()
        {
            // Arrange
            const string refreshToken = "test_refresh_token";
            
            _tokenRefreshProviderMock
                .Setup(x => x.RefreshTokenAsync(It.IsAny<TokenRefreshRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Unexpected error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenRefreshException>(() => 
                _service.RefreshTokenAsync(refreshToken));
            
            Assert.Contains("Unexpected error during token refresh", exception.Message);
            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }

        [Fact]
        public async Task IsRefreshTokenValidAsync_WithNullRefreshToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.IsRefreshTokenValidAsync(null!));
        }

        [Fact]
        public async Task IsRefreshTokenValidAsync_WithValidToken_ReturnsTrue()
        {
            // Arrange
            const string refreshToken = "test_refresh_token";

            _tokenIntrospectionServiceMock
                .Setup(x => x.IsTokenActiveAsync(refreshToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.IsRefreshTokenValidAsync(refreshToken);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsRefreshTokenValidAsync_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            const string refreshToken = "test_refresh_token";

            _tokenIntrospectionServiceMock
                .Setup(x => x.IsTokenActiveAsync(refreshToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _service.IsRefreshTokenValidAsync(refreshToken);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsRefreshTokenValidAsync_WithException_ReturnsFalse()
        {
            // Arrange
            const string refreshToken = "test_refresh_token";

            _tokenIntrospectionServiceMock
                .Setup(x => x.IsTokenActiveAsync(refreshToken, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Some error"));

            // Act
            var result = await _service.IsRefreshTokenValidAsync(refreshToken);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RefreshTokenIfNeededAsync_WithNullToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.RefreshTokenIfNeededAsync(null!));
        }

        [Fact]
        public async Task RefreshTokenIfNeededAsync_WithTokenWithoutRefreshToken_ThrowsInvalidOperationException()
        {
            // Arrange
            var token = new Token
            {
                AccessToken = "access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600
                // No refresh token
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.RefreshTokenIfNeededAsync(token));
            
            Assert.Contains("Current token does not contain a refresh token", exception.Message);
        }

        [Fact]
        public async Task RefreshTokenIfNeededAsync_WithTokenNotNeedingRefresh_ReturnsOriginalToken()
        {
            // Arrange
            var token = new Token
            {
                AccessToken = "access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600, // 1 hour - well beyond the 5 minute buffer
                RefreshToken = "refresh_token"
            };

            // Act
            var result = await _service.RefreshTokenIfNeededAsync(token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(token, result.Token);
            Assert.False(result.RefreshTokenRotated);
        }

        [Fact]
        public async Task RefreshTokenIfNeededAsync_WithTokenNeedingRefresh_RefreshesToken()
        {
            // Arrange
            var token = new Token
            {
                AccessToken = "access_token",
                TokenType = "Bearer",
                ExpiresIn = 60, // 1 minute - within the 5 minute buffer
                RefreshToken = "refresh_token"
            };

            var newToken = new Token
            {
                AccessToken = "new_access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                RefreshToken = "new_refresh_token"
            };
            var refreshResponse = TokenRefreshResponse.Success(newToken, true);

            _tokenRefreshProviderMock
                .Setup(x => x.RefreshTokenAsync(It.IsAny<TokenRefreshRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(refreshResponse);

            // Act
            var result = await _service.RefreshTokenIfNeededAsync(token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(newToken, result.Token);
            Assert.True(result.RefreshTokenRotated);
        }

        [Fact]
        public void ShouldRefreshToken_WithNullToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _service.ShouldRefreshToken(null!));
        }

        [Fact]
        public void ShouldRefreshToken_WithTokenWithoutExpirationInfo_ReturnsFalse()
        {
            // Arrange
            var token = new Token
            {
                AccessToken = "access_token",
                TokenType = "Bearer",
                ExpiresIn = 0 // No expiration info
            };

            // Act
            var result = _service.ShouldRefreshToken(token);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldRefreshToken_WithTokenWithinRefreshBuffer_ReturnsTrue()
        {
            // Arrange
            var token = new Token
            {
                AccessToken = "access_token",
                TokenType = "Bearer",
                ExpiresIn = 60 // 1 minute - within the 5 minute buffer
            };

            // Act
            var result = _service.ShouldRefreshToken(token);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ShouldRefreshToken_WithTokenBeyondRefreshBuffer_ReturnsFalse()
        {
            // Arrange
            var token = new Token
            {
                AccessToken = "access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600 // 1 hour - well beyond the 5 minute buffer
            };

            // Act
            var result = _service.ShouldRefreshToken(token);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldRefreshToken_WithKnownIssueTime_WithinRefreshBuffer_ReturnsTrue()
        {
            // Arrange
            var token = new Token
            {
                AccessToken = "access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600 // 1 hour
            };
            var issuedAt = DateTimeOffset.UtcNow.AddMinutes(-56); // Token was issued 56 minutes ago, expires in 4 minutes

            // Act
            var result = _service.ShouldRefreshToken(token, issuedAt);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ShouldRefreshToken_WithKnownIssueTime_BeyondRefreshBuffer_ReturnsFalse()
        {
            // Arrange
            var token = new Token
            {
                AccessToken = "access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600 // 1 hour
            };
            var issuedAt = DateTimeOffset.UtcNow.AddMinutes(-10); // Token was issued 10 minutes ago, expires in 50 minutes

            // Act
            var result = _service.ShouldRefreshToken(token, issuedAt);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RefreshTokenWithRetryAsync_WithNullRefreshToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.RefreshTokenWithRetryAsync(null!));
        }

        [Fact]
        public async Task RefreshTokenWithRetryAsync_WithSuccessfulFirstAttempt_ReturnsSuccess()
        {
            // Arrange
            const string refreshToken = "test_refresh_token";
            var expectedToken = new Token { AccessToken = "new_access_token" };
            var expectedResponse = TokenRefreshResponse.Success(expectedToken);

            _tokenRefreshProviderMock
                .Setup(x => x.RefreshTokenAsync(It.IsAny<TokenRefreshRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.RefreshTokenWithRetryAsync(refreshToken);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedToken, result.Token);
            
            // Verify only one attempt was made
            _tokenRefreshProviderMock.Verify(
                x => x.RefreshTokenAsync(It.IsAny<TokenRefreshRequest>(), It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task RefreshTokenWithRetryAsync_WithNonRetryableError_ReturnsFailureWithoutRetry()
        {
            // Arrange
            const string refreshToken = "test_refresh_token";
            var failureResponse = TokenRefreshResponse.Failure("Invalid refresh token", "invalid_grant");

            _tokenRefreshProviderMock
                .Setup(x => x.RefreshTokenAsync(It.IsAny<TokenRefreshRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(failureResponse);

            // Act
            var result = await _service.RefreshTokenWithRetryAsync(refreshToken);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid refresh token", result.ErrorMessage);
            Assert.Equal("invalid_grant", result.ErrorCode);
            
            // Verify only one attempt was made (no retry for non-retryable errors)
            _tokenRefreshProviderMock.Verify(
                x => x.RefreshTokenAsync(It.IsAny<TokenRefreshRequest>(), It.IsAny<CancellationToken>()), 
                Times.Once);
        }
    }
}
