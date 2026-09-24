using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Tools.Mcp;

public sealed class McpAuthenticationHandlerTests
{
    private sealed class CapturingInnerHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly CapturingInnerHandler _inner = new();

    private HttpMessageInvoker CreateSut()
    {
        var handler = new McpAuthenticationHandler(_tokenService) { InnerHandler = _inner };
        return new HttpMessageInvoker(handler);
    }

    [Fact]
    public async Task SendAsync_AttachesBearerToken_FromTokenService()
    {
        _tokenService.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("access-token-1");
        var sut = CreateSut();

        await sut.SendAsync(new HttpRequestMessage(HttpMethod.Post, "https://mcp.example.com/"), cancellationToken);

        var request = Assert.Single(_inner.Requests);
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("access-token-1", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task SendAsync_FetchesAFreshToken_OnEveryRequest()
    {
        _tokenService.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token-1", "token-2");
        var sut = CreateSut();

        await sut.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://mcp.example.com/one"), cancellationToken);
        await sut.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://mcp.example.com/two"), cancellationToken);

        Assert.Equal(2, _inner.Requests.Count);
        Assert.Equal("token-1", _inner.Requests[0].Headers.Authorization?.Parameter);
        Assert.Equal("token-2", _inner.Requests[1].Headers.Authorization?.Parameter);
        await _tokenService.Received(2).GetAccessTokenAsync(Arg.Any<CancellationToken>());
    }
}
