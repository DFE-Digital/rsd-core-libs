using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Tools.Mcp;

public sealed class TokenServiceTests
{
    private sealed class FakeTokenEndpointHandler(string accessToken, int expiresInSeconds) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            var json = $"{{\"access_token\":\"{accessToken}\",\"expires_in\":{expiresInSeconds}}}";
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    /// <summary>Fails the first <paramref name="failuresBeforeSuccess"/> calls with a 503, then succeeds.</summary>
    private sealed class FlakyTokenEndpointHandler(int failuresBeforeSuccess, string accessToken, int expiresInSeconds) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (CallCount <= failuresBeforeSuccess)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }

            var json = $"{{\"access_token\":\"{accessToken}\",\"expires_in\":{expiresInSeconds}}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class AlwaysFailingHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }
    }

    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;
    private readonly ILogger<TokenService> _logger = Substitute.For<ILogger<TokenService>>();

    private static McpServerConnectionOptions CreateOptions() => new()
    {
        ServerLabel = "my-tools",
        ServerUri = new Uri("https://mcp.example.com"),
        Authentication = new McpServerAuthenticationConfig
        {
            TenantId = "tenant-1",
            ClientId = "client-1",
            ClientSecret = "secret-1",
            Scope = "api://mcp/.default",
        },
    };

    private static TokenService CreateSut(FakeTokenEndpointHandler handler, out string tokenEndpoint)
    {
        var options = CreateOptions();
        tokenEndpoint = $"https://login.microsoftonline.com/{options.Authentication.TenantId}/oauth2/v2.0/token";
        return new TokenService(options, new HttpClient(handler));
    }

    private TokenService CreateSut(HttpMessageHandler handler) => new(CreateOptions(), new HttpClient(handler), _logger);

    [Fact]
    public async Task GetAccessTokenAsync_PostsClientCredentials_ToTheTenantsTokenEndpoint()
    {
        var handler = new FakeTokenEndpointHandler("access-token-1", expiresInSeconds: 3600);
        var sut = CreateSut(handler, out var expectedEndpoint);

        var token = await sut.GetAccessTokenAsync(cancellationToken);

        Assert.Equal("access-token-1", token);
        Assert.Equal(expectedEndpoint, handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("grant_type=client_credentials", handler.LastRequestBody);
        Assert.Contains("client_id=client-1", handler.LastRequestBody);
        Assert.Contains("client_secret=secret-1", handler.LastRequestBody);
        Assert.Contains("scope=", handler.LastRequestBody);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReusesCachedToken_WhileWellWithinItsLifetime()
    {
        var handler = new FakeTokenEndpointHandler("access-token-1", expiresInSeconds: 3600);
        var sut = CreateSut(handler, out _);

        var first = await sut.GetAccessTokenAsync(cancellationToken);
        var second = await sut.GetAccessTokenAsync(cancellationToken);

        Assert.Equal(first, second);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_RefetchesToken_OnceItIsWithinAMinuteOfExpiry()
    {
        var handler = new FakeTokenEndpointHandler("access-token-1", expiresInSeconds: 30);
        var sut = CreateSut(handler, out _);

        await sut.GetAccessTokenAsync(cancellationToken);
        await sut.GetAccessTokenAsync(cancellationToken);

        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_RetriesOnTransientFailure_AndSucceeds()
    {
        var handler = new FlakyTokenEndpointHandler(failuresBeforeSuccess: 1, "access-token-1", expiresInSeconds: 3600);
        var sut = CreateSut(handler);

        var token = await sut.GetAccessTokenAsync(cancellationToken);

        Assert.Equal("access-token-1", token);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_RetriesUpToThreeAttempts_ThenThrows_WhenTheServerNeverRecovers()
    {
        var handler = new AlwaysFailingHandler();
        var sut = CreateSut(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAccessTokenAsync(cancellationToken));

        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_PropagatesCancellation_WithoutRetrying()
    {
        var handler = new AlwaysFailingHandler();
        var sut = CreateSut(handler);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.GetAccessTokenAsync(cts.Token));

        // Cancellation must never be treated as a transient failure and retried into the full 3-attempt loop.
        Assert.True(handler.CallCount <= 1);
    }
}
