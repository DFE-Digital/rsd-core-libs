using GovUK.Dfe.CoreLibs.Security.Models;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models;

namespace GovUK.Dfe.CoreLibs.Security.Tests.TokenRefreshTests.Models
{
    public class TokenRefreshResponseTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var response = new TokenRefreshResponse();

            // Assert
            Assert.False(response.IsSuccess);
            Assert.Null(response.Token);
            Assert.Null(response.ErrorMessage);
            Assert.Null(response.ErrorCode);
            Assert.False(response.RefreshTokenRotated);
            Assert.Null(response.ExpiresAt);
            Assert.True(DateTimeOffset.UtcNow - response.RefreshedAt < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Success_WithToken_CreatesSuccessfulResponse()
        {
            // Arrange
            var token = new Token
            {
                AccessToken = "access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                RefreshToken = "refresh_token",
                Scope = "openid profile"
            };

            // Act
            var response = TokenRefreshResponse.Success(token);

            // Assert
            Assert.True(response.IsSuccess);
            Assert.Equal(token, response.Token);
            Assert.Null(response.ErrorMessage);
            Assert.Null(response.ErrorCode);
            Assert.False(response.RefreshTokenRotated);
            Assert.NotNull(response.ExpiresAt);
            Assert.True(response.ExpiresAt.Value > DateTimeOffset.UtcNow);
        }

        [Fact]
        public void Success_WithTokenRotation_SetsRotationFlag()
        {
            // Arrange
            var token = new Token
            {
                AccessToken = "access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600
            };

            // Act
            var response = TokenRefreshResponse.Success(token, refreshTokenRotated: true);

            // Assert
            Assert.True(response.IsSuccess);
            Assert.True(response.RefreshTokenRotated);
        }

        [Fact]
        public void Success_WithZeroExpiresIn_DoesNotSetExpiresAt()
        {
            // Arrange
            var token = new Token
            {
                AccessToken = "access_token",
                TokenType = "Bearer",
                ExpiresIn = 0
            };

            // Act
            var response = TokenRefreshResponse.Success(token);

            // Assert
            Assert.True(response.IsSuccess);
            Assert.Null(response.ExpiresAt);
        }

        [Fact]
        public void Failure_WithErrorMessage_CreatesFailedResponse()
        {
            // Arrange
            const string errorMessage = "Invalid refresh token";

            // Act
            var response = TokenRefreshResponse.Failure(errorMessage);

            // Assert
            Assert.False(response.IsSuccess);
            Assert.Null(response.Token);
            Assert.Equal(errorMessage, response.ErrorMessage);
            Assert.Null(response.ErrorCode);
            Assert.False(response.RefreshTokenRotated);
            Assert.Null(response.ExpiresAt);
        }

        [Fact]
        public void Failure_WithErrorMessageAndCode_CreatesFailedResponse()
        {
            // Arrange
            const string errorMessage = "Invalid refresh token";
            const string errorCode = "invalid_grant";

            // Act
            var response = TokenRefreshResponse.Failure(errorMessage, errorCode);

            // Assert
            Assert.False(response.IsSuccess);
            Assert.Null(response.Token);
            Assert.Equal(errorMessage, response.ErrorMessage);
            Assert.Equal(errorCode, response.ErrorCode);
            Assert.False(response.RefreshTokenRotated);
            Assert.Null(response.ExpiresAt);
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var response = new TokenRefreshResponse();
            var token = new Token { AccessToken = "test" };
            const string errorMessage = "Test error";
            const string errorCode = "test_error";
            var refreshedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
            var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

            // Act
            response.IsSuccess = true;
            response.Token = token;
            response.ErrorMessage = errorMessage;
            response.ErrorCode = errorCode;
            response.RefreshTokenRotated = true;
            response.RefreshedAt = refreshedAt;
            response.ExpiresAt = expiresAt;

            // Assert
            Assert.True(response.IsSuccess);
            Assert.Equal(token, response.Token);
            Assert.Equal(errorMessage, response.ErrorMessage);
            Assert.Equal(errorCode, response.ErrorCode);
            Assert.True(response.RefreshTokenRotated);
            Assert.Equal(refreshedAt, response.RefreshedAt);
            Assert.Equal(expiresAt, response.ExpiresAt);
        }
    }
}
