using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Configuration;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Exceptions;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Interfaces;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GovUK.Dfe.CoreLibs.Security.TokenRefresh.Services
{
    /// <summary>
    /// Implementation of <see cref="ITokenIntrospectionService"/> that performs OAuth 2.0 token introspection
    /// according to RFC 7662. This service validates tokens by querying the authorization server's introspection endpoint.
    /// </summary>
    public class TokenIntrospectionService : ITokenIntrospectionService
    {
        private readonly TokenRefreshOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TokenIntrospectionService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenIntrospectionService"/> class.
        /// </summary>
        /// <param name="options">The token refresh configuration options.</param>
        /// <param name="httpClientFactory">The HTTP client factory for making requests.</param>
        /// <param name="logger">The logger for this service.</param>
        public TokenIntrospectionService(
            IOptions<TokenRefreshOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<TokenIntrospectionService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _options.Validate();
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

        /// <inheritdoc/>
        public async Task<bool> IsTokenActiveAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException(nameof(token));

            try
            {
                var introspectionResponse = await IntrospectTokenAsync(token, cancellationToken);
                return introspectionResponse.Active && !introspectionResponse.IsExpired;
            }
            catch (TokenIntrospectionException ex)
            {
                _logger.LogWarning(ex, "Failed to introspect token, assuming inactive");
                return false;
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

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }
    }
}
