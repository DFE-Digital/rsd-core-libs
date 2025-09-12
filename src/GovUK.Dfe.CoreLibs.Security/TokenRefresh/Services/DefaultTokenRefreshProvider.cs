using GovUK.Dfe.CoreLibs.Security.Models;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Configuration;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Exceptions;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Interfaces;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GovUK.Dfe.CoreLibs.Security.TokenRefresh.Services
{
    /// <summary>
    /// Default implementation of <see cref="ITokenRefreshProvider"/> that provides standard OAuth 2.0 token refresh
    /// and introspection functionality. This provider can be used with most OAuth 2.0 / OpenID Connect providers
    /// that follow standard specifications.
    /// </summary>
    public class DefaultTokenRefreshProvider : ITokenRefreshProvider
    {
        private readonly TokenRefreshOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DefaultTokenRefreshProvider> _logger;

        /// <inheritdoc/>
        public string ProviderName => "DefaultOAuth2Provider";

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTokenRefreshProvider"/> class.
        /// </summary>
        /// <param name="options">The token refresh configuration options.</param>
        /// <param name="httpClientFactory">The HTTP client factory for making requests.</param>
        /// <param name="logger">The logger for this provider.</param>
        public DefaultTokenRefreshProvider(
            IOptions<TokenRefreshOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<DefaultTokenRefreshProvider> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _options.Validate();
        }

        /// <inheritdoc/>
        public async Task<TokenRefreshResponse> RefreshTokenAsync(TokenRefreshRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                throw new ArgumentException("RefreshToken cannot be null or empty", nameof(request));

            _logger.LogDebug("Starting token refresh for client {ClientId}", request.ClientId);

            try
            {
                using var httpClient = CreateHttpClient();
                var requestContent = CreateTokenRefreshRequestContent(request);

                var tokenEndpoint = string.IsNullOrWhiteSpace(request.TokenEndpoint) 
                    ? _options.TokenEndpoint 
                    : request.TokenEndpoint;

                _logger.LogDebug("Sending token refresh request to {Endpoint}", tokenEndpoint);

                var response = await httpClient.PostAsync(tokenEndpoint, requestContent, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Token refresh failed with status {StatusCode}: {Error}", 
                        response.StatusCode, responseContent);

                    var errorResponse = TryParseErrorResponse(responseContent);
                    return TokenRefreshResponse.Failure(
                        errorResponse?.ErrorDescription ?? $"Token refresh failed with status {response.StatusCode}",
                        errorResponse?.Error);
                }

                var tokenResponse = JsonSerializer.Deserialize<Token>(responseContent, GetJsonOptions());
                if (tokenResponse == null)
                {
                    _logger.LogError("Failed to deserialize token refresh response");
                    return TokenRefreshResponse.Failure("Failed to deserialize token refresh response");
                }

                // Check if refresh token was rotated (new refresh token provided)
                var refreshTokenRotated = !string.IsNullOrEmpty(tokenResponse.RefreshToken) 
                    && tokenResponse.RefreshToken != request.RefreshToken;

                _logger.LogDebug("Token refresh completed successfully. Refresh token rotated: {Rotated}", refreshTokenRotated);
                return TokenRefreshResponse.Success(tokenResponse, refreshTokenRotated);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during token refresh");
                throw new TokenRefreshException("HTTP error during token refresh", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Token refresh request timed out");
                throw new TokenRefreshException("Token refresh request timed out", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize token refresh response");
                throw new TokenRefreshException("Failed to deserialize token refresh response", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<TokenIntrospectionResponse> IntrospectTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException(nameof(token));

            _logger.LogDebug("Starting token introspection");

            try
            {
                using var httpClient = CreateHttpClient();
                var requestContent = CreateIntrospectionRequestContent(token);

                _logger.LogDebug("Sending introspection request to {Endpoint}", _options.IntrospectionEndpoint);

                var response = await httpClient.PostAsync(_options.IntrospectionEndpoint, requestContent, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Token introspection failed with status {StatusCode}: {Error}", 
                        response.StatusCode, errorContent);
                    
                    throw new TokenIntrospectionException(
                        $"Token introspection failed with status {response.StatusCode}: {errorContent}",
                        (int)response.StatusCode);
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var introspectionResponse = JsonSerializer.Deserialize<TokenIntrospectionResponse>(responseContent, GetJsonOptions());

                if (introspectionResponse == null)
                {
                    _logger.LogError("Failed to deserialize introspection response");
                    throw new TokenIntrospectionException("Failed to deserialize introspection response");
                }

                _logger.LogDebug("Token introspection completed. Token active: {Active}", introspectionResponse.Active);
                return introspectionResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during token introspection");
                throw new TokenIntrospectionException("HTTP error during token introspection", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Token introspection request timed out");
                throw new TokenIntrospectionException("Token introspection request timed out", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize token introspection response");
                throw new TokenIntrospectionException("Failed to deserialize token introspection response", ex);
            }
        }

        private HttpClient CreateHttpClient()
        {
            var httpClient = string.IsNullOrWhiteSpace(_options.HttpClientName)
                ? _httpClientFactory.CreateClient()
                : _httpClientFactory.CreateClient(_options.HttpClientName);

            httpClient.Timeout = _options.HttpTimeout;

            // Add additional headers if configured
            foreach (var header in _options.AdditionalHeaders)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            return httpClient;
        }

        private FormUrlEncodedContent CreateTokenRefreshRequestContent(TokenRefreshRequest request)
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "refresh_token"),
                new("refresh_token", request.RefreshToken),
                new("client_id", request.ClientId ?? _options.ClientId)
            };

            // Add client secret if provided
            var clientSecret = request.ClientSecret ?? _options.ClientSecret;
            if (!string.IsNullOrWhiteSpace(clientSecret))
            {
                parameters.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
            }

            // Add scope if provided
            var scope = request.Scope ?? _options.DefaultScope;
            if (!string.IsNullOrWhiteSpace(scope))
            {
                parameters.Add(new KeyValuePair<string, string>("scope", scope));
            }

            // Add any additional parameters
            foreach (var param in request.AdditionalParameters)
            {
                parameters.Add(new KeyValuePair<string, string>(param.Key, param.Value));
            }

            return new FormUrlEncodedContent(parameters);
        }

        private FormUrlEncodedContent CreateIntrospectionRequestContent(string token)
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new("token", token),
                new("client_id", _options.ClientId),
                new("client_secret", _options.ClientSecret)
            };

            return new FormUrlEncodedContent(parameters);
        }

        private static TokenErrorResponse? TryParseErrorResponse(string responseContent)
        {
            try
            {
                return JsonSerializer.Deserialize<TokenErrorResponse>(responseContent, GetJsonOptions());
            }
            catch
            {
                return null;
            }
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        /// <summary>
        /// Represents an OAuth 2.0 error response.
        /// </summary>
        private class TokenErrorResponse
        {
            public string? Error { get; set; }
            public string? ErrorDescription { get; set; }
            public string? ErrorUri { get; set; }
        }
    }
}
