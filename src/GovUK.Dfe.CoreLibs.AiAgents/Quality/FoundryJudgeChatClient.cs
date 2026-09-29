using Azure.AI.Extensions.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using System.Runtime.CompilerServices;

namespace GovUK.Dfe.CoreLibs.AiAgents.Quality;

/// <summary>
/// Evaulates a chat with a Foundry judge model, which is a single-turn model that returns the whole answer at once. It does not support streaming.
/// </summary>
/// <param name="client">The OpenAI client for interacting with the Foundry judge model.</param>
/// <param name="model">The model to use for evaluation.</param>
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

    /// <summary>
    /// Returns a streaming response from the Foundry judge model. Since the Foundry judge model does not support streaming, this method will yield a single update containing the complete response.
    /// </summary>
    /// <param name="messages">The messages to send to the model.</param>
    /// <param name="options">The options for the chat request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of chat response updates.</returns>
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
