using Azure.Core;
using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;

/// <summary>
/// Gets and caches an Entra ID access token for one MCP server, from its configured
/// <see cref="McpServerAuthenticationConfig.Credential"/> or, failing that, the client-secret flow.
/// Safe to call concurrently: only one refresh runs at a time.
/// </summary>
public class TokenService(McpServerConnectionOptions mcpServerConnectionOptions, HttpClient httpClient,
    ILogger<TokenService>? logger = null) : ITokenService
{
    internal const string HttpClientName = "GovUK.Dfe.CoreLibs.AiAgents.TokenService";

    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500)];
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(1);

    private readonly ILogger<TokenService> _logger = logger ?? NullLogger<TokenService>.Instance;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private volatile CachedToken? _cached;

    private McpServerAuthenticationConfig Authentication => mcpServerConnectionOptions.Authentication;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is { } current && current.IsFresh)
        {
            return current.AccessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cached is { } refreshed && refreshed.IsFresh)
            {
                return refreshed.AccessToken;
            }

            var token = await RequestTokenWithRetryAsync(cancellationToken).ConfigureAwait(false);
            _cached = token;
            return token.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<CachedToken> RequestTokenWithRetryAsync(CancellationToken cancellationToken)
    {
        var maxAttempts = RetryDelays.Length + 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return Authentication.Credential is { } credential
                    ? await RequestTokenFromCredentialAsync(credential, cancellationToken).ConfigureAwait(false)
                    : await RequestTokenWithClientSecretAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (attempt == maxAttempts || !IsTransient(ex))
                {
                    _logger.LogError(ex, "Failed to acquire an MCP access token for server {ServerLabel} after {MaxAttempts} attempts.",
                        mcpServerConnectionOptions.ServerLabel, maxAttempts);
                    throw;
                }

                _logger.LogWarning(ex,
                    "Failed to acquire an MCP access token for server {ServerLabel} (attempt {Attempt}/{MaxAttempts}); retrying.",
                    mcpServerConnectionOptions.ServerLabel, attempt, maxAttempts);
                await Task.Delay(RetryDelays[attempt - 1], cancellationToken).ConfigureAwait(false);
            }
        }

        throw new UnreachableException();
    }

    /// <summary>
    /// Only failures that can succeed on a second attempt are retried: network errors, server errors and
    /// throttling. A wrong secret, a missing permission or a bad request fails straight away.
    /// </summary>
    internal static bool IsTransient(Exception exception) => exception switch
    {
        Azure.Identity.AuthenticationFailedException or Azure.Identity.CredentialUnavailableException => false,
        HttpRequestException { StatusCode: null } => true,
        HttpRequestException { StatusCode: { } status } => (int)status >= 500 || status == System.Net.HttpStatusCode.TooManyRequests,
        Azure.RequestFailedException failed => failed.Status == 0 || failed.Status >= 500 || failed.Status == 429,
        _ => false,
    };

    private async Task<CachedToken> RequestTokenFromCredentialAsync(TokenCredential credential, CancellationToken cancellationToken)
    {
        var token = await credential.GetTokenAsync(new TokenRequestContext([Authentication.Scope]), cancellationToken).ConfigureAwait(false);
        return new CachedToken(token.Token, token.ExpiresOn);
    }

    private async Task<CachedToken> RequestTokenWithClientSecretAsync(CancellationToken cancellationToken)
    {
        var tokenEndpoint = new Uri(Authentication.AuthorityHost, $"{Authentication.TenantId}/oauth2/v2.0/token");

        using var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = Authentication.ClientId ?? string.Empty,
            ["client_secret"] = Authentication.ClientSecret ?? string.Empty,
            ["scope"] = Authentication.Scope,
        });

        using var response = await httpClient.PostAsync(tokenEndpoint, body, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException(ErrorMessages.McpTokenResponseDeserializationFailed);
        return new CachedToken(token.AccessToken, DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn));
    }

    private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresOn)
    {
        public bool IsFresh => DateTimeOffset.UtcNow < ExpiresOn - RefreshMargin;
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn
    );
}
