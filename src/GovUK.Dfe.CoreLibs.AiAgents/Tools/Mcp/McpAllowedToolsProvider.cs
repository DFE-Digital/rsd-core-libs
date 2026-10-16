using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;

/// <summary>Gives an agent a subset of an MCP server's tools, and runs only that subset when the model calls them.</summary>
/// <param name="allowedToolNames">The tool names this agent may see and call.</param>
public sealed class McpAllowedToolsProvider(IMcpToolClient client, IReadOnlyList<string> allowedToolNames)
    : IAgentToolProvider, IAgentToolExecutor
{
    private readonly HashSet<string> _allowedFunctionNames = [.. allowedToolNames.Select(McpToolClient.ToFunctionName)];

    public Task<IReadOnlyList<ResponseTool>> GetToolsAsync(CancellationToken cancellationToken = default)
        => client.GetToolsAsync(allowedToolNames, cancellationToken);

    public async Task<string?> TryExecuteAsync(ToolCallRequest call, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(call);

        return _allowedFunctionNames.Contains(call.FunctionName)
            ? await client.CallToolAsync(call.FunctionName, call.Arguments, cancellationToken).ConfigureAwait(false)
            : null;
    }
}
