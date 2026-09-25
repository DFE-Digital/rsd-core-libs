using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;

/// <summary>
/// Validates a named MCP server's configuration and connection at application startup.
/// </summary>
/// <param name="serverKey">The key this server was registered under (via <c>AddMcpClientServices</c>), used in log messages.</param>
/// <param name="client">The MCP client for this server.</param>
/// <param name="options">This server's connection options, checked for missing/empty required fields.</param>
/// <param name="logger"></param>
public sealed class McpToolStartupValidator(string serverKey, IMcpToolClient client, McpServerConnectionOptions options,
    ILogger<McpToolStartupValidator> logger) : IHostedService
{
    /// <summary>
    /// Validates the MCP server's configuration, then its connection and configured tools, at application startup.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task representing the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            options.Validate(serverKey);
            await client.GetToolsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "MCP tool configuration validation failed during startup for server '{ServerKey}'.", serverKey);
            throw new InvalidOperationException(string.Format(ErrorMessages.McpStartupValidationFailed, serverKey), ex);
        }
    }
    /// <summary>
    /// Stops the hosted service. This implementation does nothing and returns a completed task.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task representing the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
