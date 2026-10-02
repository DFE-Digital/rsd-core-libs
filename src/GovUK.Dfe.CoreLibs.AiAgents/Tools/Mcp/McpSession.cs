using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;

/// <summary>
/// One live connection to an MCP server. <see cref="McpToolClient"/> replaces it when it stops working,
/// so a server restart or an expired session doesn't break every later call until the app restarts.
/// </summary>
internal interface IMcpSession : IAsyncDisposable
{
    Task<IReadOnlyList<Tool>> ListToolsAsync(CancellationToken cancellationToken);

    Task<CallToolResult> CallToolAsync(string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken);

    Task<GetPromptResult> GetPromptAsync(string name, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken);
}

/// <summary>An <see cref="IMcpSession"/> over the MCP SDK's client.</summary>
internal sealed class McpClientSession(McpClient client) : IMcpSession
{
    public async Task<IReadOnlyList<Tool>> ListToolsAsync(CancellationToken cancellationToken)
        => [.. (await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).Select(tool => tool.ProtocolTool)];

    public Task<CallToolResult> CallToolAsync(string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
        => client.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken).AsTask();

    public Task<GetPromptResult> GetPromptAsync(string name, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
        => client.GetPromptAsync(name, arguments, cancellationToken: cancellationToken).AsTask();

    public ValueTask DisposeAsync() => client.DisposeAsync();
}
