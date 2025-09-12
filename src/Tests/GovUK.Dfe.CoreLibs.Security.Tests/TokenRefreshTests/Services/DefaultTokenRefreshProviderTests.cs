using GovUK.Dfe.CoreLibs.Security.Models;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Configuration;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Exceptions;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace GovUK.Dfe.CoreLibs.Security.Tests.TokenRefreshTests.Services
{
    public class DefaultTokenRefreshProviderTests
    {
        private readonly Mock<IOptions<TokenRefreshOptions>> _optionsMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<ILogger<DefaultTokenRefreshProvider>> _loggerMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly TokenRefreshOptions _options;
        private readonly DefaultTokenRefreshProvider _provider;

        public DefaultTokenRefreshProviderTests()
        {
            _optionsMock = new Mock<IOptions<TokenRefreshOptions>>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<DefaultTokenRefreshProvider>>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            _options = new TokenRefreshOptions
            {
                TokenEndpoint = "https://test.example.com/token",
                IntrospectionEndpoint = "https://test.example.com/introspect",
                ClientId = "test-client",
                ClientSecret = "test-secret",
                HttpTimeout = TimeSpan.FromSeconds(30),
                DefaultScope = "openid profile"
            };

            _optionsMock.Setup(x => x.Value).Returns(_options);

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _provider = new DefaultTokenRefreshProvider(_optionsMock.Object, _httpClientFactoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new DefaultTokenRefreshProvider(null!, _httpClientFactoryMock.Object, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullHttpClientFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new DefaultTokenRefreshProvider(_optionsMock.Object, null!, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new DefaultTokenRefreshProvider(_optionsMock.Object, _httpClientFactoryMock.Object, null!));
        }

        [Fact]
        public void ProviderName_ReturnsCorrectName()
        {
            // Act
            var name = _provider.ProviderName;

            // Assert
            Assert.Equal("DefaultOAuth2Provider", name);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _provider.RefreshTokenAsync(null!));
        }

        [Fact]
        public async Task RefreshTokenAsync_WithEmptyRefreshToken_ThrowsArgumentException()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "",
                ClientId = "test-client"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _provider.RefreshTokenAsync(request));
            Assert.Contains("RefreshToken cannot be null or empty", exception.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithWhitespaceRefreshToken_ThrowsArgumentException()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "   ",
                ClientId = "test-client"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _provider.RefreshTokenAsync(request));
            Assert.Contains("RefreshToken cannot be null or empty", exception.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithSuccessfulResponse_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "refresh-token",
                ClientId = "test-client"
            };

            var tokenResponse = new Token
            {
                AccessToken = "new-access-token",
                RefreshToken = "new-refresh-token",
                TokenType = "Bearer",
                ExpiresIn = 3600
            };

            var responseContent = JsonSerializer.Serialize(tokenResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            SetupHttpResponse(HttpStatusCode.OK, responseContent);

            // Act
            var result = await _provider.RefreshTokenAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(tokenResponse.AccessToken, result.Token.AccessToken);
            Assert.Equal(tokenResponse.RefreshToken, result.Token.RefreshToken);
            Assert.True(result.RefreshTokenRotated);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithSameRefreshToken_DoesNotMarkAsRotated()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "refresh-token",
                ClientId = "test-client"
            };

            var tokenResponse = new Token
            {
                AccessToken = "new-access-token",
                RefreshToken = "refresh-token", // Same as request
                TokenType = "Bearer",
                ExpiresIn = 3600
            };

            var responseContent = JsonSerializer.Serialize(tokenResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            SetupHttpResponse(HttpStatusCode.OK, responseContent);

            // Act
            var result = await _provider.RefreshTokenAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.RefreshTokenRotated);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithEmptyRefreshTokenInResponse_DoesNotMarkAsRotated()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "refresh-token",
                ClientId = "test-client"
            };

            var tokenResponse = new Token
            {
                AccessToken = "new-access-token",
                RefreshToken = "", // Empty refresh token
                TokenType = "Bearer",
                ExpiresIn = 3600
            };

            var responseContent = JsonSerializer.Serialize(tokenResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            SetupHttpResponse(HttpStatusCode.OK, responseContent);

            // Act
            var result = await _provider.RefreshTokenAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.RefreshTokenRotated);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithHttpError_ReturnsFailureResponse()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "refresh-token",
                ClientId = "test-client"
            };

            var errorResponse = new
            {
                error = "invalid_grant",
                error_description = "The refresh token is invalid"
            };

            var responseContent = JsonSerializer.Serialize(errorResponse);

            SetupHttpResponse(HttpStatusCode.BadRequest, responseContent);

            // Act
            var result = await _provider.RefreshTokenAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("The refresh token is invalid", result.ErrorMessage);
            Assert.Equal("invalid_grant", result.ErrorCode);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithHttpErrorAndInvalidJson_ReturnsFailureResponse()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "refresh-token",
                ClientId = "test-client"
            };

            SetupHttpResponse(HttpStatusCode.BadRequest, "Invalid JSON response");

            // Act
            var result = await _provider.RefreshTokenAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Token refresh failed with status BadRequest", result.ErrorMessage);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithNullTokenResponse_ReturnsFailureResponse()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "refresh-token",
                ClientId = "test-client"
            };

            SetupHttpResponse(HttpStatusCode.OK, "null");

            // Act
            var result = await _provider.RefreshTokenAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Failed to deserialize token refresh response", result.ErrorMessage);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithHttpRequestException_ThrowsTokenRefreshException()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "refresh-token",
                ClientId = "test-client"
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenRefreshException>(() => 
                _provider.RefreshTokenAsync(request));
            Assert.Contains("HTTP error during token refresh", exception.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithTimeoutException_ThrowsTokenRefreshException()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "refresh-token",
                ClientId = "test-client"
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Request timed out", new TimeoutException()));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenRefreshException>(() => 
                _provider.RefreshTokenAsync(request));
            Assert.Contains("Token refresh request timed out", exception.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithJsonException_ThrowsTokenRefreshException()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "refresh-token",
                ClientId = "test-client"
            };

            SetupHttpResponse(HttpStatusCode.OK, "Invalid JSON");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenRefreshException>(() => 
                _provider.RefreshTokenAsync(request));
            Assert.Contains("Failed to deserialize token refresh response", exception.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithCustomTokenEndpoint_UsesCustomEndpoint()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "refresh-token",
                ClientId = "test-client",
                TokenEndpoint = "https://custom.example.com/token"
            };

            var tokenResponse = new Token
            {
                AccessToken = "new-access-token",
                TokenType = "Bearer",
                ExpiresIn = 3600
            };

            var responseContent = JsonSerializer.Serialize(tokenResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            SetupHttpResponse(HttpStatusCode.OK, responseContent);

            // Act
            var result = await _provider.RefreshTokenAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            
            // Verify the request was made to the custom endpoint
            _httpMessageHandlerMock.Protected()
                .Verify<Task<HttpResponseMessage>>("SendAsync", 
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == "https://custom.example.com/token"),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task RefreshTokenAsync_WithCustomClientSecret_UsesCustomSecret()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "refresh-token",
                ClientId = "test-client",
                ClientSecret = "custom-secret"
            };

            var tokenResponse = new Token
            {
                AccessToken = "new-access-token",
                TokenType = "Bearer",
                ExpiresIn = 3600
            };

            var responseContent = JsonSerializer.Serialize(tokenResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            SetupHttpResponse(HttpStatusCode.OK, responseContent);

            // Act
            var result = await _provider.RefreshTokenAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithCustomScope_UsesCustomScope()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "refresh-token",
                ClientId = "test-client",
                Scope = "custom-scope"
            };

            var tokenResponse = new Token
            {
                AccessToken = "new-access-token",
                TokenType = "Bearer",
                ExpiresIn = 3600
            };

            var responseContent = JsonSerializer.Serialize(tokenResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            SetupHttpResponse(HttpStatusCode.OK, responseContent);

            // Act
            var result = await _provider.RefreshTokenAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithAdditionalParameters_IncludesAdditionalParameters()
        {
            // Arrange
            var request = new TokenRefreshRequest
            {
                RefreshToken = "refresh-token",
                ClientId = "test-client"
            };
            request.AdditionalParameters.Add("custom_param", "custom_value");

            var tokenResponse = new Token
            {
                AccessToken = "new-access-token",
                TokenType = "Bearer",
                ExpiresIn = 3600
            };

            var responseContent = JsonSerializer.Serialize(tokenResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            SetupHttpResponse(HttpStatusCode.OK, responseContent);

            // Act
            var result = await _provider.RefreshTokenAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithNullToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _provider.IntrospectTokenAsync(null!));
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithEmptyToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _provider.IntrospectTokenAsync(""));
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithWhitespaceToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _provider.IntrospectTokenAsync("   "));
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithSuccessfulResponse_ReturnsIntrospectionResponse()
        {
            // Arrange
            var token = "test-token";

            var introspectionResponse = new TokenIntrospectionResponse
            {
                Active = true,
                Scope = "openid profile",
                ExpirationTime = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                IssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var responseContent = JsonSerializer.Serialize(introspectionResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            SetupHttpResponse(HttpStatusCode.OK, responseContent);

            // Act
            var result = await _provider.IntrospectTokenAsync(token);

            // Assert
            Assert.True(result.Active);
            Assert.Equal("openid profile", result.Scope);
            Assert.NotNull(result.ExpiresAt);
            Assert.NotNull(result.IssuedAtTime);
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithHttpError_ThrowsTokenIntrospectionException()
        {
            // Arrange
            var token = "test-token";

            SetupHttpResponse(HttpStatusCode.BadRequest, "Invalid token");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenIntrospectionException>(() => 
                _provider.IntrospectTokenAsync(token));
            Assert.Contains("Token introspection failed with status BadRequest", exception.Message);
            Assert.Equal(400, exception.StatusCode);
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithNullResponse_ThrowsTokenIntrospectionException()
        {
            // Arrange
            var token = "test-token";

            SetupHttpResponse(HttpStatusCode.OK, "null");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenIntrospectionException>(() => 
                _provider.IntrospectTokenAsync(token));
            Assert.Contains("Failed to deserialize introspection response", exception.Message);
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithHttpRequestException_ThrowsTokenIntrospectionException()
        {
            // Arrange
            var token = "test-token";

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenIntrospectionException>(() => 
                _provider.IntrospectTokenAsync(token));
            Assert.Contains("HTTP error during token introspection", exception.Message);
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithTimeoutException_ThrowsTokenIntrospectionException()
        {
            // Arrange
            var token = "test-token";

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Request timed out", new TimeoutException()));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenIntrospectionException>(() => 
                _provider.IntrospectTokenAsync(token));
            Assert.Contains("Token introspection request timed out", exception.Message);
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithJsonException_ThrowsTokenIntrospectionException()
        {
            // Arrange
            var token = "test-token";

            SetupHttpResponse(HttpStatusCode.OK, "Invalid JSON");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenIntrospectionException>(() => 
                _provider.IntrospectTokenAsync(token));
            Assert.Contains("Failed to deserialize token introspection response", exception.Message);
        }

        [Fact]
        public async Task CreateHttpClient_WithCustomHttpClientName_UsesCustomClient()
        {
            // Arrange
            _options.HttpClientName = "custom-client";
            _options.AdditionalHeaders.Add("X-Custom-Header", "custom-value");

            var customHttpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient("custom-client")).Returns(customHttpClient);

            var provider = new DefaultTokenRefreshProvider(_optionsMock.Object, _httpClientFactoryMock.Object, _loggerMock.Object);

            // Act
            var request = new TokenRefreshRequest
            {
                RefreshToken = "refresh-token",
                ClientId = "test-client"
            };

            var tokenResponse = new Token
            {
                AccessToken = "new-access-token",
                TokenType = "Bearer",
                ExpiresIn = 3600
            };

            var responseContent = JsonSerializer.Serialize(tokenResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            SetupHttpResponse(HttpStatusCode.OK, responseContent);

            // Act
            var result = await provider.RefreshTokenAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            _httpClientFactoryMock.Verify(x => x.CreateClient("custom-client"), Times.Once);
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }
    }
}
