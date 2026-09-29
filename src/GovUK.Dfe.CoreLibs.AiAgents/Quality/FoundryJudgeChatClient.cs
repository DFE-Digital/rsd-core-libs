using Azure.AI.Extensions.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using System.Runtime.CompilerServices;

namespace GovUK.Dfe.CoreLibs.AiAgents.Quality;

/// <summary>
/// Uses the Foundry Judge API to get a response to a chat conversation. This is used for quality assurance of AI agents, and is not intended for production use.   
/// </summary>
/// <param name="client">This app's Foundry project client.</param>
/// <param name="model">The judge model, e.g. "myconnection/gpt-5.1".</param>
public sealed class FoundryJudgeChatClient(ProjectOpenAIClient client, string model) : IChatClient
{
    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var request = new CreateResponseOptions(model, messages.Select(ToResponseItem));
        var response = (await client.GetProjectResponsesClient().CreateResponseAsync(request, cancellationToken).ConfigureAwait(false)).Value;

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, response.GetOutputText()))
        {
            ModelId = response.Model,
            Usage = response.Usage is { } usage
                ? new UsageDetails { InputTokenCount = usage.InputTokenCount, OutputTokenCount = usage.OutputTokenCount, TotalTokenCount = usage.TotalTokenCount }
                : null,
        };
    }

    /// <summary>Returns the whole answer as one update: this client doesn't stream, and judges don't need it.</summary>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        foreach (var update in response.ToChatResponseUpdates())
        {
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
        // The Foundry client is shared and owned by the container.
    }

    private static ResponseItem ToResponseItem(ChatMessage message)
    {
        if (message.Role == ChatRole.System)
        {
            return ResponseItem.CreateSystemMessageItem(message.Text);
        }

        return message.Role == ChatRole.Assistant
            ? ResponseItem.CreateAssistantMessageItem(message.Text, annotations: null)
            : ResponseItem.CreateUserMessageItem(message.Text);
    }
}
