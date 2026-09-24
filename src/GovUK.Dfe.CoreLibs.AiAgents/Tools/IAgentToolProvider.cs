using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tools;

/// <summary>
/// Provides tools available to an agent.
/// </summary>
public interface IAgentToolProvider
{
    /// <summary>
    /// Gets the available agent tools.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The available tools.</returns>
    Task<IReadOnlyList<ResponseTool>> GetToolsAsync(CancellationToken cancellationToken = default);
}
