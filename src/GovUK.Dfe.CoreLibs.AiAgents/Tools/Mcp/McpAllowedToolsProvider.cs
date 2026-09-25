using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;

/// <summary>
/// Provides a set of tools to an agent, filtered by a list of allowed tool names retrieved from the MCP tool client.
/// </summary>
/// <param name="client">The MCP tool client to retrieve tools from.</param>
/// <param name="allowedToolNames">The list of allowed tool names.</param>
public sealed class McpAllowedToolsProvider(IMcpToolClient client, IReadOnlyList<string> allowedToolNames) : IAgentToolProvider
{
    public Task<IReadOnlyList<ResponseTool>> GetToolsAsync(CancellationToken cancellationToken = default)
        => client.GetToolsAsync(allowedToolNames, cancellationToken);
}
