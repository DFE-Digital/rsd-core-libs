using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using System.Net.Http.Headers;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;

/// <summary>
/// A delegating HTTP handler that adds a bearer token to the Authorization header of outgoing HTTP requests.
/// </summary>
/// <param name="tokenService"></param>
public sealed class McpAuthenticationHandler(ITokenService tokenService) : DelegatingHandler
{
    /// <summary>
    /// Sends an HTTP request with a bearer token added to the Authorization header.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenService.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
