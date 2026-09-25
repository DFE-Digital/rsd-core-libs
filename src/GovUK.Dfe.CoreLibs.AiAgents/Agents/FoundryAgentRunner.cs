using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI.Responses;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

public sealed class FoundryAgentRunner(IAgentFactory agentFactory, IFoundryConversationClient conversationClient,
    ILogger<FoundryAgentRunner>? logger = null) : IAgentRunner
{
    /// <summary>
    /// The maximum number of tool-call rounds allowed per agent run.
    /// </summary>
    private const int MaxToolCallRounds = 10;

    private readonly ILogger<FoundryAgentRunner> _logger = logger ?? NullLogger<FoundryAgentRunner>.Instance;


    public async Task<AgentResult> RunAsync(AgentSpec spec, string prompt, string? conversationId = null,
        Func<IReadOnlyList<ToolCallRequest>, CancellationToken, Task<IEnumerable<ToolCallOutput>>>? resolveToolCalls = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        cancellationToken.ThrowIfCancellationRequested();

        var agent = await agentFactory.GetOrCreateAsync(spec, cancellationToken).ConfigureAwait(false);

        return await RunCoreAsync(agent, prompt, conversationId, additionalContext: null, resolveToolCalls, cancellationToken)
            .ConfigureAwait(false);
    }

    
    public async Task<AgentResult> RunAsync(AgentReference agent, string prompt, string? conversationId = null,
        string? additionalContext = null,
        Func<IReadOnlyList<ToolCallRequest>, CancellationToken, Task<IEnumerable<ToolCallOutput>>>? resolveToolCalls = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        cancellationToken.ThrowIfCancellationRequested();

        return await RunCoreAsync(agent, prompt, conversationId, additionalContext, resolveToolCalls, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<AgentResult> RunCoreAsync(AgentReference agent, string prompt, string? conversationId,
        string? additionalContext,
        Func<IReadOnlyList<ToolCallRequest>, CancellationToken, Task<IEnumerable<ToolCallOutput>>>? resolveToolCalls,
        CancellationToken cancellationToken)
    {
        var resolvedConversationId = conversationId
            ?? await conversationClient.CreateConversationAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            IReadOnlyList<ResponseItem> inputItems = BuildInitialInputItems(prompt, additionalContext);
            ResponseResult response;
            var round = 0;

            // Responses return synchronously, unlike Threads/Runs - no poll loop is needed. The
            // only reason to call again is a function tool call the model is waiting on.
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (++round > MaxToolCallRounds)
                {
                    throw new InvalidOperationException(
                        string.Format(ErrorMessages.ToolCallRoundLimitExceeded, agent.Name, MaxToolCallRounds));
                }

                response = await conversationClient.CreateResponseAsync(agent.Name, resolvedConversationId, inputItems, agent.Version, cancellationToken)
                    .ConfigureAwait(false);

                var toolCalls = response.OutputItems.OfType<FunctionCallResponseItem>().ToList();
                if (toolCalls.Count == 0)
                {
                    ThrowIfNotCompleted(agent.Name, response);
                    break;
                }

                inputItems = await ResolveToolCallsAsync(response, toolCalls, resolveToolCalls, cancellationToken).ConfigureAwait(false);
            }

            var output = response.GetOutputText();
            var totalTokens = response.Usage?.TotalTokenCount ?? 0;

            return new AgentResult(agent.Name, output, totalTokens);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Agent {AgentName} failed", agent.Name);
            throw new InvalidOperationException(string.Format(ErrorMessages.AgentRunFailed, agent.Name), ex);
        }
    }
     
    private static IReadOnlyList<ResponseItem> BuildInitialInputItems(string prompt, string? additionalContext)
        => additionalContext is null
            ? [ResponseItem.CreateUserMessageItem(prompt)]
            : [ResponseItem.CreateDeveloperMessageItem(additionalContext), ResponseItem.CreateUserMessageItem(prompt)];
     
    private static void ThrowIfNotCompleted(string agentName, ResponseResult response)
    {
        if (response.Status is null || response.Status == ResponseStatus.Completed)
        {
            return;
        }

        var detail = string.Empty;
        if (response.Error is not null)
        {
            detail = $": {response.Error.Message}";
        }
        else if (response.IncompleteStatusDetails?.Reason is { } reason)
        {
            detail = $": {reason}";
        }

        throw new InvalidOperationException(
            string.Format(ErrorMessages.ResponseDidNotComplete, agentName, response.Id, response.Status, detail));
    }

    private static async Task<IReadOnlyList<ResponseItem>> ResolveToolCallsAsync(ResponseResult response,
        IReadOnlyList<FunctionCallResponseItem> toolCalls,
        Func<IReadOnlyList<ToolCallRequest>, CancellationToken, Task<IEnumerable<ToolCallOutput>>>? resolveToolCalls,
        CancellationToken cancellationToken)
    {
        if (resolveToolCalls is null)
        {
            throw new InvalidOperationException(string.Format(ErrorMessages.ToolCallsRequiredNoCallback, response.Id));
        }

        var requests = toolCalls
            .Select(call => new ToolCallRequest(call.CallId, call.FunctionName, call.FunctionArguments.ToString()))
            .ToList();
        var outputs = await resolveToolCalls(requests, cancellationToken).ConfigureAwait(false);
        var outputByCallId = outputs.ToDictionary(o => o.CallId, o => o.Output);

        var nextInputItems = new List<ResponseItem>(response.OutputItems);
        foreach (var callId in toolCalls.Select(toolCall => toolCall.CallId))
        {
            if (!outputByCallId.TryGetValue(callId, out var output))
            {
                throw new InvalidOperationException(string.Format(ErrorMessages.MissingToolCallOutput, callId));
            }

            nextInputItems.Add(ResponseItem.CreateFunctionCallOutputItem(callId, output));
        }

        return nextInputItems;
    }
}
