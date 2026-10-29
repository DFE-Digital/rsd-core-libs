using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>Foundry conversations: create one, send input to an agent in it, delete it.</summary>
public interface IFoundryConversationClient
{
    /// <summary>Creates a conversation and returns its ID.</summary>
    Task<string> CreateConversationAsync(CancellationToken cancellationToken = default);

    /// <summary>Sends input to an agent in a conversation and returns its response.</summary>
    /// <param name="agentVersion">The version to run; null runs the latest.</param>
    /// <param name="maxOutputTokens">The most output tokens this response may use; null for the model's own limit.</param>
    Task<ResponseResult> CreateResponseAsync(string agentName, string conversationId, IReadOnlyList<ResponseItem> inputItems,
        string? agentVersion = null, int? maxOutputTokens = null, CancellationToken cancellationToken = default);

    /// <summary>Deletes a conversation.</summary>
    Task DeleteConversationAsync(string conversationId, CancellationToken cancellationToken = default);
}
