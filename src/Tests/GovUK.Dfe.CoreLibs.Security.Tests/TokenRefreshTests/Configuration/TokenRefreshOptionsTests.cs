using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Configuration;

namespace GovUK.Dfe.CoreLibs.Security.Tests.TokenRefreshTests.Configuration
{
    public class TokenRefreshOptionsTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var options = new TokenRefreshOptions();

            // Assert
            Assert.Equal(5, options.RefreshBufferMinutes);
            Assert.True(options.EnableBackgroundRefresh);
            Assert.Equal(TimeSpan.FromMinutes(1), options.RefreshCheckInterval);
            Assert.Equal(TimeSpan.FromSeconds(30), options.HttpTimeout);
            Assert.Equal(3, options.MaxRetryAttempts);
            Assert.Equal(TimeSpan.FromSeconds(1), options.RetryDelay);
            Assert.Equal(TimeSpan.FromMinutes(5), options.IntrospectionCacheLifetime);
            Assert.True(options.ValidateSslCertificates);
            Assert.NotNull(options.AdditionalHeaders);
            Assert.Empty(options.AdditionalHeaders);
            Assert.Null(options.HttpClientName);
            Assert.Null(options.DefaultScope);
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var options = new TokenRefreshOptions();
            const string tokenEndpoint = "https://example.com/token";
            const string introspectionEndpoint = "https://example.com/introspect";
            const string clientId = "test_client";
            const string clientSecret = "test_secret";
            const int refreshBufferMinutes = 10;
            var refreshCheckInterval = TimeSpan.FromMinutes(2);
            var httpTimeout = TimeSpan.FromSeconds(60);
            const int maxRetryAttempts = 5;
            var retryDelay = TimeSpan.FromSeconds(2);
            var cacheLifetime = TimeSpan.FromMinutes(10);
            const string httpClientName = "CustomClient";
            const string defaultScope = "openid profile";

            // Act
            options.TokenEndpoint = tokenEndpoint;
            options.IntrospectionEndpoint = introspectionEndpoint;
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
            options.RefreshBufferMinutes = refreshBufferMinutes;
            options.EnableBackgroundRefresh = false;
            options.RefreshCheckInterval = refreshCheckInterval;
            options.HttpTimeout = httpTimeout;
            options.MaxRetryAttempts = maxRetryAttempts;
            options.RetryDelay = retryDelay;
            options.IntrospectionCacheLifetime = cacheLifetime;
            options.ValidateSslCertificates = false;
            options.HttpClientName = httpClientName;
            options.DefaultScope = defaultScope;

            // Assert
            Assert.Equal(tokenEndpoint, options.TokenEndpoint);
            Assert.Equal(introspectionEndpoint, options.IntrospectionEndpoint);
            Assert.Equal(clientId, options.ClientId);
            Assert.Equal(clientSecret, options.ClientSecret);
            Assert.Equal(refreshBufferMinutes, options.RefreshBufferMinutes);
            Assert.False(options.EnableBackgroundRefresh);
            Assert.Equal(refreshCheckInterval, options.RefreshCheckInterval);
            Assert.Equal(httpTimeout, options.HttpTimeout);
            Assert.Equal(maxRetryAttempts, options.MaxRetryAttempts);
            Assert.Equal(retryDelay, options.RetryDelay);
            Assert.Equal(cacheLifetime, options.IntrospectionCacheLifetime);
            Assert.False(options.ValidateSslCertificates);
            Assert.Equal(httpClientName, options.HttpClientName);
            Assert.Equal(defaultScope, options.DefaultScope);
        }

        [Fact]
        public void RefreshBuffer_ReturnsCorrectTimeSpan()
        {
            // Arrange
            var options = new TokenRefreshOptions
            {
                RefreshBufferMinutes = 15
            };

            // Act
            var refreshBuffer = options.RefreshBuffer;

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(15), refreshBuffer);
        }

        [Fact]
        public void AdditionalHeaders_CanBeModified()
        {
            // Arrange
            var options = new TokenRefreshOptions();
            const string headerName = "X-Custom-Header";
            const string headerValue = "custom-value";

            // Act
            options.AdditionalHeaders[headerName] = headerValue;

            // Assert
            Assert.True(options.AdditionalHeaders.ContainsKey(headerName));
            Assert.Equal(headerValue, options.AdditionalHeaders[headerName]);
        }

        [Fact]
        public void Validate_WithValidConfiguration_DoesNotThrow()
        {
            // Arrange
            var options = new TokenRefreshOptions
            {
                TokenEndpoint = "https://example.com/token",
                IntrospectionEndpoint = "https://example.com/introspect",
                ClientId = "test_client",
                ClientSecret = "test_secret"
            };

            // Act & Assert
            options.Validate(); // Should not throw
        }

        [Fact]
        public void Validate_WithEmptyTokenEndpoint_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = new TokenRefreshOptions
            {
                TokenEndpoint = "",
                IntrospectionEndpoint = "https://example.com/introspect",
                ClientId = "test_client",
                ClientSecret = "test_secret"
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
            Assert.Contains("TokenEndpoint is required", exception.Message);
        }

        [Fact]
        public void Validate_WithNullTokenEndpoint_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = new TokenRefreshOptions
            {
                TokenEndpoint = null!,
                IntrospectionEndpoint = "https://example.com/introspect",
                ClientId = "test_client",
                ClientSecret = "test_secret"
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
            Assert.Contains("TokenEndpoint is required", exception.Message);
        }

        [Fact]
        public void Validate_WithEmptyIntrospectionEndpoint_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = new TokenRefreshOptions
            {
                TokenEndpoint = "https://example.com/token",
                IntrospectionEndpoint = "",
                ClientId = "test_client",
                ClientSecret = "test_secret"
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
            Assert.Contains("IntrospectionEndpoint is required", exception.Message);
        }

        [Fact]
        public void Validate_WithEmptyClientId_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = new TokenRefreshOptions
            {
                TokenEndpoint = "https://example.com/token",
                IntrospectionEndpoint = "https://example.com/introspect",
                ClientId = "",
                ClientSecret = "test_secret"
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
            Assert.Contains("ClientId is required", exception.Message);
        }

        [Fact]
        public void Validate_WithEmptyClientSecret_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = new TokenRefreshOptions
            {
                TokenEndpoint = "https://example.com/token",
                IntrospectionEndpoint = "https://example.com/introspect",
                ClientId = "test_client",
                ClientSecret = ""
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
            Assert.Contains("ClientSecret is required", exception.Message);
        }

        [Fact]
        public void Validate_WithInvalidTokenEndpointUri_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = new TokenRefreshOptions
            {
                TokenEndpoint = "not-a-valid-uri",
                IntrospectionEndpoint = "https://example.com/introspect",
                ClientId = "test_client",
                ClientSecret = "test_secret"
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
            Assert.Contains("TokenEndpoint must be a valid absolute URI", exception.Message);
        }

        [Fact]
        public void Validate_WithInvalidIntrospectionEndpointUri_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = new TokenRefreshOptions
            {
                TokenEndpoint = "https://example.com/token",
                IntrospectionEndpoint = "not-a-valid-uri",
                ClientId = "test_client",
                ClientSecret = "test_secret"
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
            Assert.Contains("IntrospectionEndpoint must be a valid absolute URI", exception.Message);
        }

        [Fact]
        public void Validate_WithNegativeRefreshBufferMinutes_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = new TokenRefreshOptions
            {
                TokenEndpoint = "https://example.com/token",
                IntrospectionEndpoint = "https://example.com/introspect",
                ClientId = "test_client",
                ClientSecret = "test_secret",
                RefreshBufferMinutes = -1
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
            Assert.Contains("RefreshBufferMinutes cannot be negative", exception.Message);
        }

        [Fact]
        public void Validate_WithZeroHttpTimeout_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = new TokenRefreshOptions
            {
                TokenEndpoint = "https://example.com/token",
                IntrospectionEndpoint = "https://example.com/introspect",
                ClientId = "test_client",
                ClientSecret = "test_secret",
                HttpTimeout = TimeSpan.Zero
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
            Assert.Contains("HttpTimeout must be greater than zero", exception.Message);
        }

        [Fact]
        public void Validate_WithNegativeMaxRetryAttempts_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = new TokenRefreshOptions
            {
                TokenEndpoint = "https://example.com/token",
                IntrospectionEndpoint = "https://example.com/introspect",
                ClientId = "test_client",
                ClientSecret = "test_secret",
                MaxRetryAttempts = -1
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
            Assert.Contains("MaxRetryAttempts cannot be negative", exception.Message);
        }

        [Fact]
        public void Validate_WithNegativeRetryDelay_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = new TokenRefreshOptions
            {
                TokenEndpoint = "https://example.com/token",
                IntrospectionEndpoint = "https://example.com/introspect",
                ClientId = "test_client",
                ClientSecret = "test_secret",
                RetryDelay = TimeSpan.FromSeconds(-1)
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
            Assert.Contains("RetryDelay cannot be negative", exception.Message);
        }

        [Fact]
        public void Validate_WithZeroIntrospectionCacheLifetime_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = new TokenRefreshOptions
            {
                TokenEndpoint = "https://example.com/token",
                IntrospectionEndpoint = "https://example.com/introspect",
                ClientId = "test_client",
                ClientSecret = "test_secret",
                IntrospectionCacheLifetime = TimeSpan.Zero
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
            Assert.Contains("IntrospectionCacheLifetime must be greater than zero", exception.Message);
        }
    }
}
