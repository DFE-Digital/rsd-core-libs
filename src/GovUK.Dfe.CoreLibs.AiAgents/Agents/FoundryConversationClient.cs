using Azure.AI.Extensions.OpenAI;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

public sealed class FoundryConversationClient(ProjectOpenAIClient client) : IFoundryConversationClient
{
    public async Task<string> CreateConversationAsync(CancellationToken cancellationToken = default)
    {
        var conversationsClient = client.GetProjectConversationsClient();
        var response = await conversationsClient.CreateProjectConversationAsync(
            new ProjectConversationCreationOptions(), cancellationToken).ConfigureAwait(false);
        return response.Value.Id;
    }

    public async Task<ResponseResult> CreateResponseAsync(string agentName, string conversationId,
        IReadOnlyList<ResponseItem> inputItems, string? agentVersion = null, CancellationToken cancellationToken = default)
    {
        var agentReference = new AgentReference(agentName, version: agentVersion ?? "");
        var responsesClient = client.GetProjectResponsesClientForAgent(agentReference, conversationId);

        var options = new CreateResponseOptions();
        foreach (var item in inputItems)
        {
            options.InputItems.Add(item);
        }

        var response = await responsesClient.CreateResponseAsync(options, cancellationToken).ConfigureAwait(false);
        return response.Value;
    }
}
