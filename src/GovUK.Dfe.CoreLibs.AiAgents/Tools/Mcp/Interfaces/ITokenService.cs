namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;

/// <summary>
/// Provides access tokens for external service authentication.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Gets an access token.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The access token.</returns>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
