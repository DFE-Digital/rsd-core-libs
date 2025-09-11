using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models;
using System.Text.Json;

namespace GovUK.Dfe.CoreLibs.Security.Tests.TokenRefreshTests.Models
{
    public class TokenIntrospectionResponseTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var response = new TokenIntrospectionResponse();

            // Assert
            Assert.False(response.Active);
            Assert.Null(response.Subject);
            Assert.Null(response.ClientId);
            Assert.Null(response.ExpirationTime);
            Assert.Null(response.IssuedAt);
            Assert.Null(response.Issuer);
            Assert.Null(response.Scope);
            Assert.Null(response.TokenType);
            Assert.Null(response.Username);
            Assert.Null(response.AdditionalClaims);
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var response = new TokenIntrospectionResponse();
            const string subject = "user123";
            const string clientId = "client456";
            const long expirationTime = 1640995200; // 2022-01-01
            const long issuedAt = 1640991600; // 2022-01-01 - 1 hour
            const string issuer = "https://example.com";
            const string scope = "openid profile";
            const string tokenType = "Bearer";
            const string username = "testuser";

            // Act
            response.Active = true;
            response.Subject = subject;
            response.ClientId = clientId;
            response.ExpirationTime = expirationTime;
            response.IssuedAt = issuedAt;
            response.Issuer = issuer;
            response.Scope = scope;
            response.TokenType = tokenType;
            response.Username = username;

            // Assert
            Assert.True(response.Active);
            Assert.Equal(subject, response.Subject);
            Assert.Equal(clientId, response.ClientId);
            Assert.Equal(expirationTime, response.ExpirationTime);
            Assert.Equal(issuedAt, response.IssuedAt);
            Assert.Equal(issuer, response.Issuer);
            Assert.Equal(scope, response.Scope);
            Assert.Equal(tokenType, response.TokenType);
            Assert.Equal(username, response.Username);
        }

        [Fact]
        public void ExpiresAt_WithExpirationTime_ReturnsCorrectDateTimeOffset()
        {
            // Arrange
            var response = new TokenIntrospectionResponse
            {
                ExpirationTime = 1640995200 // 2022-01-01 00:00:00 UTC
            };

            // Act
            var expiresAt = response.ExpiresAt;

            // Assert
            Assert.NotNull(expiresAt);
            Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), expiresAt.Value);
        }

        [Fact]
        public void ExpiresAt_WithoutExpirationTime_ReturnsNull()
        {
            // Arrange
            var response = new TokenIntrospectionResponse();

            // Act
            var expiresAt = response.ExpiresAt;

            // Assert
            Assert.Null(expiresAt);
        }

        [Fact]
        public void IssuedAtTime_WithIssuedAt_ReturnsCorrectDateTimeOffset()
        {
            // Arrange
            var response = new TokenIntrospectionResponse
            {
                IssuedAt = 1640991600 // 2021-12-31 23:00:00 UTC
            };

            // Act
            var issuedAtTime = response.IssuedAtTime;

            // Assert
            Assert.NotNull(issuedAtTime);
            Assert.Equal(new DateTimeOffset(2021, 12, 31, 23, 0, 0, TimeSpan.Zero), issuedAtTime.Value);
        }

        [Fact]
        public void IssuedAtTime_WithoutIssuedAt_ReturnsNull()
        {
            // Arrange
            var response = new TokenIntrospectionResponse();

            // Act
            var issuedAtTime = response.IssuedAtTime;

            // Assert
            Assert.Null(issuedAtTime);
        }

        [Fact]
        public void IsExpired_WithFutureExpirationTime_ReturnsFalse()
        {
            // Arrange
            var futureTime = DateTimeOffset.UtcNow.AddHours(1);
            var response = new TokenIntrospectionResponse
            {
                ExpirationTime = futureTime.ToUnixTimeSeconds()
            };

            // Act
            var isExpired = response.IsExpired;

            // Assert
            Assert.False(isExpired);
        }

        [Fact]
        public void IsExpired_WithPastExpirationTime_ReturnsTrue()
        {
            // Arrange
            var pastTime = DateTimeOffset.UtcNow.AddHours(-1);
            var response = new TokenIntrospectionResponse
            {
                ExpirationTime = pastTime.ToUnixTimeSeconds()
            };

            // Act
            var isExpired = response.IsExpired;

            // Assert
            Assert.True(isExpired);
        }

        [Fact]
        public void IsExpired_WithoutExpirationTime_ReturnsFalse()
        {
            // Arrange
            var response = new TokenIntrospectionResponse();

            // Act
            var isExpired = response.IsExpired;

            // Assert
            Assert.False(isExpired);
        }

        [Fact]
        public void Scopes_WithScopeString_ReturnsCorrectArray()
        {
            // Arrange
            var response = new TokenIntrospectionResponse
            {
                Scope = "openid profile email"
            };

            // Act
            var scopes = response.Scopes;

            // Assert
            Assert.Equal(3, scopes.Length);
            Assert.Contains("openid", scopes);
            Assert.Contains("profile", scopes);
            Assert.Contains("email", scopes);
        }

        [Fact]
        public void Scopes_WithEmptyScope_ReturnsEmptyArray()
        {
            // Arrange
            var response = new TokenIntrospectionResponse
            {
                Scope = ""
            };

            // Act
            var scopes = response.Scopes;

            // Assert
            Assert.Empty(scopes);
        }

        [Fact]
        public void Scopes_WithNullScope_ReturnsEmptyArray()
        {
            // Arrange
            var response = new TokenIntrospectionResponse
            {
                Scope = null
            };

            // Act
            var scopes = response.Scopes;

            // Assert
            Assert.Empty(scopes);
        }

        [Fact]
        public void Scopes_WithWhitespaceScope_ReturnsEmptyArray()
        {
            // Arrange
            var response = new TokenIntrospectionResponse
            {
                Scope = "   "
            };

            // Act
            var scopes = response.Scopes;

            // Assert
            Assert.Empty(scopes);
        }

        [Fact]
        public void JsonSerialization_WorksCorrectly()
        {
            // Arrange
            var response = new TokenIntrospectionResponse
            {
                Active = true,
                Subject = "user123",
                ClientId = "client456",
                ExpirationTime = 1640995200,
                IssuedAt = 1640991600,
                Issuer = "https://example.com",
                Scope = "openid profile",
                TokenType = "Bearer"
            };

            // Act
            var json = JsonSerializer.Serialize(response);
            var deserializedResponse = JsonSerializer.Deserialize<TokenIntrospectionResponse>(json);

            // Assert
            Assert.NotNull(deserializedResponse);
            Assert.Equal(response.Active, deserializedResponse.Active);
            Assert.Equal(response.Subject, deserializedResponse.Subject);
            Assert.Equal(response.ClientId, deserializedResponse.ClientId);
            Assert.Equal(response.ExpirationTime, deserializedResponse.ExpirationTime);
            Assert.Equal(response.IssuedAt, deserializedResponse.IssuedAt);
            Assert.Equal(response.Issuer, deserializedResponse.Issuer);
            Assert.Equal(response.Scope, deserializedResponse.Scope);
            Assert.Equal(response.TokenType, deserializedResponse.TokenType);
        }
    }
}
