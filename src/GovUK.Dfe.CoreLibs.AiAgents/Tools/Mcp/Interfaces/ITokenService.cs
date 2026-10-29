namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;

/// <summary>Gets an access token for one MCP server.</summary>
public interface ITokenService
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
