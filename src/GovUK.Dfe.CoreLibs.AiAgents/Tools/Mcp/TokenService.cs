using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;

public class TokenService(McpServerConnectionOptions mcpServerConnectionOptions, HttpClient httpClient,
    ILogger<TokenService>? logger = null) : ITokenService
{
    internal const string HttpClientName = "GovUK.Dfe.CoreLibs.AiAgents.TokenService";

    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500)];

    private readonly ILogger<TokenService> _logger = logger ?? NullLogger<TokenService>.Instance;

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry.AddMinutes(-1))
        {
            return _cachedToken;
        }

        var token = await RequestTokenWithRetryAsync(cancellationToken).ConfigureAwait(false);

        _cachedToken = token.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(token.ExpiresIn);

        return _cachedToken;
    }

    private async Task<TokenResponse> RequestTokenWithRetryAsync(CancellationToken cancellationToken)
    {
        var maxAttempts = RetryDelays.Length + 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await RequestTokenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (attempt == maxAttempts)
                {
                    _logger.LogError(ex, "Failed to acquire an MCP access token for tenant {TenantId} after {MaxAttempts} attempts.",
                        mcpServerConnectionOptions.Authentication.TenantId, maxAttempts);
                    throw;
                }

                _logger.LogWarning(ex,
                    "Failed to acquire an MCP access token for tenant {TenantId} (attempt {Attempt}/{MaxAttempts}); retrying.",
                    mcpServerConnectionOptions.Authentication.TenantId, attempt, maxAttempts);
                await Task.Delay(RetryDelays[attempt - 1], cancellationToken).ConfigureAwait(false);
            }
        }

        throw new UnreachableException();
    }

    private async Task<TokenResponse> RequestTokenAsync(CancellationToken cancellationToken)
    {
        var tokenEndpoint = $"https://login.microsoftonline.com/{mcpServerConnectionOptions.Authentication.TenantId}/oauth2/v2.0/token";

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = mcpServerConnectionOptions.Authentication.ClientId,
            ["client_secret"] = mcpServerConnectionOptions.Authentication.ClientSecret,
            ["scope"] = mcpServerConnectionOptions.Authentication.Scope
        });

        var response = await httpClient.PostAsync(tokenEndpoint, body, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException(ErrorMessages.McpTokenResponseDeserializationFailed);
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn
    );
}
