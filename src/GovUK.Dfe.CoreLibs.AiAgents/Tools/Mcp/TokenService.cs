using Azure.Core;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;

/// <summary>
/// Gets an MCP server's access token from the app's credential and caches it until a minute before it expires.
/// One refresh runs at a time; transient failures are retried, a refused credential isn't.
/// </summary>
public sealed class TokenService(TokenCredential credential, string scope, ILogger<TokenService>? logger = null)
    : ITokenService, IDisposable
{
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500)];
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(1);

    private readonly ILogger<TokenService> _logger = logger ?? NullLogger<TokenService>.Instance;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private volatile CachedToken? _cached;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is { IsFresh: true } current)
        {
            return current.AccessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cached is { IsFresh: true } refreshed)
            {
                return refreshed.AccessToken;
            }

            _cached = await RequestTokenWithRetryAsync(cancellationToken).ConfigureAwait(false);
            return _cached.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public void Dispose() => _refreshLock.Dispose();

    private async Task<CachedToken> RequestTokenWithRetryAsync(CancellationToken cancellationToken)
    {
        var maxAttempts = RetryDelays.Length + 1;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var token = await credential.GetTokenAsync(new TokenRequestContext([scope]), cancellationToken).ConfigureAwait(false);
                return new CachedToken(token.Token, token.ExpiresOn);
            }
            catch (Exception ex) when (ex is not OperationCanceledException && attempt < maxAttempts && IsTransient(ex))
            {
                _logger.LogWarning(ex, "Failed to get an MCP token for {Scope} (attempt {Attempt}/{MaxAttempts}); retrying",
                    scope, attempt, maxAttempts);
                await Task.Delay(RetryDelays[attempt - 1], cancellationToken).ConfigureAwait(false);
            }
        }

        throw new UnreachableException();
    }

    /// <summary>Network errors, server errors and throttling. A wrong secret or missing permission isn't.</summary>
    internal static bool IsTransient(Exception exception) => exception switch
    {
        Azure.Identity.AuthenticationFailedException or Azure.Identity.CredentialUnavailableException => false,
        HttpRequestException { StatusCode: null } => true,
        HttpRequestException { StatusCode: { } status } => (int)status >= 500 || status == System.Net.HttpStatusCode.TooManyRequests,
        Azure.RequestFailedException failed => failed.Status == 0 || failed.Status >= 500 || failed.Status == 429,
        _ => false,
    };

    private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresOn)
    {
        public bool IsFresh => DateTimeOffset.UtcNow < ExpiresOn - RefreshMargin;
    }
}
