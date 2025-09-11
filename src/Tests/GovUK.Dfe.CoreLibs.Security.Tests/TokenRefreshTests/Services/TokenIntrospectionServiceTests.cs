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

namespace GovUK.Dfe.CoreLibs.Security.Tests.TokenRefreshTests.Services
{
    public class TokenIntrospectionServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<ILogger<TokenIntrospectionService>> _loggerMock;
        private readonly TokenRefreshOptions _options;
        private readonly TokenIntrospectionService _service;

        public TokenIntrospectionServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<TokenIntrospectionService>>();
            _options = new TokenRefreshOptions
            {
                TokenEndpoint = "https://example.com/token",
                IntrospectionEndpoint = "https://example.com/introspect",
                ClientId = "test_client",
                ClientSecret = "test_secret"
            };
            
            var optionsMock = new Mock<IOptions<TokenRefreshOptions>>();
            optionsMock.Setup(x => x.Value).Returns(_options);

            _service = new TokenIntrospectionService(optionsMock.Object, _httpClientFactoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TokenIntrospectionService(null!, _httpClientFactoryMock.Object, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullHttpClientFactory_ThrowsArgumentNullException()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<TokenRefreshOptions>>();
            optionsMock.Setup(x => x.Value).Returns(_options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TokenIntrospectionService(optionsMock.Object, null!, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<TokenRefreshOptions>>();
            optionsMock.Setup(x => x.Value).Returns(_options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TokenIntrospectionService(optionsMock.Object, _httpClientFactoryMock.Object, null!));
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithNullToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.IntrospectTokenAsync(null!));
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithEmptyToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.IntrospectTokenAsync(""));
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithWhitespaceToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.IntrospectTokenAsync("   "));
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithValidToken_ReturnsIntrospectionResponse()
        {
            // Arrange
            const string token = "test_token";
            var expectedResponse = new TokenIntrospectionResponse
            {
                Active = true,
                Subject = "user123",
                ClientId = "test_client",
                ExpirationTime = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                Scope = "openid profile"
            };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse), Encoding.UTF8, "application/json")
            };

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var result = await _service.IntrospectTokenAsync(token);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Active, result.Active);
            Assert.Equal(expectedResponse.Subject, result.Subject);
            Assert.Equal(expectedResponse.ClientId, result.ClientId);
            Assert.Equal(expectedResponse.ExpirationTime, result.ExpirationTime);
            Assert.Equal(expectedResponse.Scope, result.Scope);
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithHttpError_ThrowsTokenIntrospectionException()
        {
            // Arrange
            const string token = "test_token";
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request", Encoding.UTF8, "text/plain")
            };

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenIntrospectionException>(() => 
                _service.IntrospectTokenAsync(token));
            
            Assert.Contains("Token introspection failed with status BadRequest", exception.Message);
            Assert.Equal(400, exception.StatusCode);
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithInvalidJson_ThrowsTokenIntrospectionException()
        {
            // Arrange
            const string token = "test_token";
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("invalid json", Encoding.UTF8, "application/json")
            };

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenIntrospectionException>(() => 
                _service.IntrospectTokenAsync(token));
            
            Assert.Contains("Failed to deserialize token introspection response", exception.Message);
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithNullJsonResponse_ThrowsTokenIntrospectionException()
        {
            // Arrange
            const string token = "test_token";
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            };

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenIntrospectionException>(() => 
                _service.IntrospectTokenAsync(token));
            
            Assert.Contains("Failed to deserialize introspection response", exception.Message);
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithHttpRequestException_ThrowsTokenIntrospectionException()
        {
            // Arrange
            const string token = "test_token";
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TokenIntrospectionException>(() => 
                _service.IntrospectTokenAsync(token));
            
            Assert.Contains("HTTP error during token introspection", exception.Message);
            Assert.IsType<HttpRequestException>(exception.InnerException);
        }

        [Fact]
        public async Task IsTokenActiveAsync_WithNullToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.IsTokenActiveAsync(null!));
        }

        [Fact]
        public async Task IsTokenActiveAsync_WithActiveToken_ReturnsTrue()
        {
            // Arrange
            const string token = "test_token";
            var introspectionResponse = new TokenIntrospectionResponse
            {
                Active = true,
                ExpirationTime = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(introspectionResponse), Encoding.UTF8, "application/json")
            };

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var result = await _service.IsTokenActiveAsync(token);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsTokenActiveAsync_WithInactiveToken_ReturnsFalse()
        {
            // Arrange
            const string token = "test_token";
            var introspectionResponse = new TokenIntrospectionResponse
            {
                Active = false
            };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(introspectionResponse), Encoding.UTF8, "application/json")
            };

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var result = await _service.IsTokenActiveAsync(token);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsTokenActiveAsync_WithExpiredToken_ReturnsFalse()
        {
            // Arrange
            const string token = "test_token";
            var introspectionResponse = new TokenIntrospectionResponse
            {
                Active = true,
                ExpirationTime = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds() // Expired
            };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(introspectionResponse), Encoding.UTF8, "application/json")
            };

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var result = await _service.IsTokenActiveAsync(token);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsTokenActiveAsync_WithException_ReturnsFalse()
        {
            // Arrange
            const string token = "test_token";
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var result = await _service.IsTokenActiveAsync(token);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IntrospectTokenAsync_WithAdditionalHeaders_IncludesHeaders()
        {
            // Arrange
            const string token = "test_token";
            _options.AdditionalHeaders["X-Custom-Header"] = "custom-value";
            
            var introspectionResponse = new TokenIntrospectionResponse { Active = true };
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(introspectionResponse), Encoding.UTF8, "application/json")
            };

            HttpRequestMessage? capturedRequest = null;
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
                .ReturnsAsync(httpResponseMessage);

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            await _service.IntrospectTokenAsync(token);

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.True(capturedRequest.Headers.Contains("X-Custom-Header"));
            Assert.Equal("custom-value", capturedRequest.Headers.GetValues("X-Custom-Header").First());
        }
    }
}
