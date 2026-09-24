using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>
/// Provides access to Foundry conversations, including creating conversations and sending input to agents.
/// </summary>
public interface IFoundryConversationClient
{
    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ID of the created conversation.</returns>
    Task<string> CreateConversationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends input items to an agent in a conversation and returns the agent's response.
    /// </summary>
    /// <param name="agentName">The name of the agent to send input to.</param>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <param name="inputItems">The input items to send.</param>
    /// <param name="agentVersion">A specific version to pin the call to, or null to use whatever Foundry currently considers the latest version of <paramref name="agentName"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response from the agent.</returns>

    Task<ResponseResult> CreateResponseAsync(string agentName, string conversationId, IReadOnlyList<ResponseItem> inputItems,
        string? agentVersion = null, CancellationToken cancellationToken = default);
}
