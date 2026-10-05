using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using System.Net.Http.Headers;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;

/// <summary>Adds the MCP server's bearer token, from <see cref="ITokenService"/>, to every request.</summary>
public sealed class McpAuthenticationHandler(ITokenService tokenService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenService.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
