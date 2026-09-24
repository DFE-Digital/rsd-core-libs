using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;

/// <summary>
/// Discovers tools and prompts from a remote MCP server.
/// </summary>
public interface IMcpToolClient : IAgentToolProvider, IAsyncDisposable
{
   /// <summary>
   /// Discovers tools on the MCP server, restricted to <paramref name="allowedToolNames"/> (or every
   /// </summary>
   /// <param name="allowedToolNames">The tool names to restrict this call to, or null/empty for all tools the server reports.</param>
   /// <param name="cancellationToken">The cancellation token.</param>
   /// <returns>A list of available tools.</returns>
    Task<IReadOnlyList<ResponseTool>> GetToolsAsync(IReadOnlyList<string>? allowedToolNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a prompt from the MCP server.
    /// </summary>
    /// <param name="name">The prompt name.</param>
    /// <param name="promptType">The prompt type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The prompt text.</returns>
    Task<string> GetPromptAsync(string name, string promptType, CancellationToken cancellationToken = default);
}
