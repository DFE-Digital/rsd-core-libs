using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models;

namespace GovUK.Dfe.CoreLibs.Security.Tests.TokenRefreshTests.Models
{
    public class RefreshTokenInfoTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var info = new RefreshTokenInfo();

            // Assert
            Assert.False(info.IsValid);
            Assert.Null(info.ExpiresAt);
            Assert.Null(info.Scope);
            Assert.Null(info.ClientId);
            Assert.Null(info.Subject);
            Assert.NotNull(info.Metadata);
            Assert.Empty(info.Metadata);
            Assert.True(DateTimeOffset.UtcNow - info.LastVerified < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var info = new RefreshTokenInfo();
            const string refreshToken = "refresh_token_123";
            const string scope = "openid profile";
            const string clientId = "client_456";
            const string subject = "user_789";
            var expiresAt = DateTimeOffset.UtcNow.AddDays(30);
            var lastVerified = DateTimeOffset.UtcNow.AddMinutes(-5);

            // Act
            info.RefreshToken = refreshToken;
            info.IsValid = true;
            info.ExpiresAt = expiresAt;
            info.Scope = scope;
            info.ClientId = clientId;
            info.Subject = subject;
            info.LastVerified = lastVerified;

            // Assert
            Assert.Equal(refreshToken, info.RefreshToken);
            Assert.True(info.IsValid);
            Assert.Equal(expiresAt, info.ExpiresAt);
            Assert.Equal(scope, info.Scope);
            Assert.Equal(clientId, info.ClientId);
            Assert.Equal(subject, info.Subject);
            Assert.Equal(lastVerified, info.LastVerified);
        }

        [Fact]
        public void Metadata_CanBeModified()
        {
            // Arrange
            var info = new RefreshTokenInfo();
            const string key = "custom_claim";
            object value = "custom_value";

            // Act
            info.Metadata[key] = value;

            // Assert
            Assert.True(info.Metadata.ContainsKey(key));
            Assert.Equal(value, info.Metadata[key]);
        }

        [Fact]
        public void IsExpired_WithFutureExpirationTime_ReturnsFalse()
        {
            // Arrange
            var info = new RefreshTokenInfo
            {
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            };

            // Act
            var isExpired = info.IsExpired;

            // Assert
            Assert.False(isExpired);
        }

        [Fact]
        public void IsExpired_WithPastExpirationTime_ReturnsTrue()
        {
            // Arrange
            var info = new RefreshTokenInfo
            {
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1)
            };

            // Act
            var isExpired = info.IsExpired;

            // Assert
            Assert.True(isExpired);
        }

        [Fact]
        public void IsExpired_WithoutExpirationTime_ReturnsFalse()
        {
            // Arrange
            var info = new RefreshTokenInfo();

            // Act
            var isExpired = info.IsExpired;

            // Assert
            Assert.False(isExpired);
        }

        [Fact]
        public void NeedsReverification_WithinCacheLifetime_ReturnsFalse()
        {
            // Arrange
            var info = new RefreshTokenInfo
            {
                LastVerified = DateTimeOffset.UtcNow.AddMinutes(-2)
            };
            var cacheLifetime = TimeSpan.FromMinutes(5);

            // Act
            var needsReverification = info.NeedsReverification(cacheLifetime);

            // Assert
            Assert.False(needsReverification);
        }

        [Fact]
        public void NeedsReverification_BeyondCacheLifetime_ReturnsTrue()
        {
            // Arrange
            var info = new RefreshTokenInfo
            {
                LastVerified = DateTimeOffset.UtcNow.AddMinutes(-10)
            };
            var cacheLifetime = TimeSpan.FromMinutes(5);

            // Act
            var needsReverification = info.NeedsReverification(cacheLifetime);

            // Assert
            Assert.True(needsReverification);
        }

        [Fact]
        public void NeedsReverification_ExactlyCacheLifetime_ReturnsTrue()
        {
            // Arrange
            var info = new RefreshTokenInfo
            {
                LastVerified = DateTimeOffset.UtcNow.AddMinutes(-5)
            };
            var cacheLifetime = TimeSpan.FromMinutes(5);

            // Act
            var needsReverification = info.NeedsReverification(cacheLifetime);

            // Assert
            Assert.True(needsReverification);
        }

        [Fact]
        public void FromIntrospectionResponse_CreatesCorrectRefreshTokenInfo()
        {
            // Arrange
            const string refreshToken = "refresh_token_123";
            var introspectionResponse = new TokenIntrospectionResponse
            {
                Active = true,
                Subject = "user_123",
                ClientId = "client_456",
                ExpirationTime = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds(),
                Scope = "openid profile email",
                AdditionalClaims = new Dictionary<string, object>
                {
                    { "custom_claim", "custom_value" }
                }
            };

            // Act
            var info = RefreshTokenInfo.FromIntrospectionResponse(refreshToken, introspectionResponse);

            // Assert
            Assert.Equal(refreshToken, info.RefreshToken);
            Assert.Equal(introspectionResponse.Active, info.IsValid);
            Assert.Equal(introspectionResponse.ExpiresAt, info.ExpiresAt);
            Assert.Equal(introspectionResponse.Scope, info.Scope);
            Assert.Equal(introspectionResponse.ClientId, info.ClientId);
            Assert.Equal(introspectionResponse.Subject, info.Subject);
            Assert.True(DateTimeOffset.UtcNow - info.LastVerified < TimeSpan.FromSeconds(1));
            Assert.Equal(introspectionResponse.AdditionalClaims, info.Metadata);
        }

        [Fact]
        public void FromIntrospectionResponse_WithNullAdditionalClaims_CreatesEmptyMetadata()
        {
            // Arrange
            const string refreshToken = "refresh_token_123";
            var introspectionResponse = new TokenIntrospectionResponse
            {
                Active = true,
                AdditionalClaims = null
            };

            // Act
            var info = RefreshTokenInfo.FromIntrospectionResponse(refreshToken, introspectionResponse);

            // Assert
            Assert.NotNull(info.Metadata);
            Assert.Empty(info.Metadata);
        }

        [Fact]
        public void FromIntrospectionResponse_WithInactiveToken_SetsValidToFalse()
        {
            // Arrange
            const string refreshToken = "refresh_token_123";
            var introspectionResponse = new TokenIntrospectionResponse
            {
                Active = false
            };

            // Act
            var info = RefreshTokenInfo.FromIntrospectionResponse(refreshToken, introspectionResponse);

            // Assert
            Assert.False(info.IsValid);
        }
    }
}
