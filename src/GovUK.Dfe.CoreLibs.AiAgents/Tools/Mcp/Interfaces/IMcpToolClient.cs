using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;

/// <summary>
/// Discovers tools and prompts on a remote MCP server and runs its tools from this app. Foundry only
/// ever sees function definitions and never contacts the server, so its credentials stay in this app.
/// </summary>
public interface IMcpToolClient : IAgentToolProvider, IAgentToolExecutor, IAsyncDisposable
{
    /// <summary>
    /// Describes the server's tools as function tools, restricted to <paramref name="allowedToolNames"/>
    /// (or every tool the server reports, when null/empty).
    /// </summary>
    /// <param name="allowedToolNames">The tool names to restrict this call to, or null/empty for all tools the server reports.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of available tools.</returns>
    Task<IReadOnlyList<ResponseTool>> GetToolsAsync(IReadOnlyList<string>? allowedToolNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a tool on the MCP server with this app's credentials.
    /// </summary>
    /// <param name="functionName">The function name the model used (the tool's name, made safe for a function name).</param>
    /// <param name="argumentsJson">The call's arguments as a JSON object.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tool's text output.</returns>
    Task<string> CallToolAsync(string functionName, string argumentsJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a prompt from the MCP server.
    /// </summary>
    /// <param name="name">The prompt name.</param>
    /// <param name="promptType">The prompt type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The prompt text.</returns>
    Task<string> GetPromptAsync(string name, string promptType, CancellationToken cancellationToken = default);
}
