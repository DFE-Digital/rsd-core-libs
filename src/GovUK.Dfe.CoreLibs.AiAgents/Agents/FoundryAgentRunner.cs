using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Concurrency;
using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.Diagnostics;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI.Responses;
using System.Diagnostics;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

public sealed class FoundryAgentRunner(IAgentFactory agentFactory, IFoundryConversationClient conversationClient,
    ILogger<FoundryAgentRunner>? logger = null, AgentRunOptions? runOptions = null, IAgentRunLimiter? runLimiter = null) : IAgentRunner
{
    /// <summary>The most tool-call rounds in one run.</summary>
    private const int MaxToolCallRounds = 10;

    /// <summary>How long conversation clean-up may take once a run has finished (or been cancelled).</summary>
    private static readonly TimeSpan ConversationCleanupTimeout = TimeSpan.FromSeconds(30);

    private readonly ILogger<FoundryAgentRunner> _logger = logger ?? NullLogger<FoundryAgentRunner>.Instance;
    private readonly AgentRunOptions _runOptions = runOptions ?? new AgentRunOptions();

    public async Task<AgentResult> RunAsync(AgentSpec spec, string prompt, string? conversationId = null,
        ToolCallResolver? resolveToolCalls = null,
        string? additionalContext = null, Func<AgentResult, string?>? validateOutput = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        cancellationToken.ThrowIfCancellationRequested();

        var agent = await agentFactory.GetOrCreateAsync(spec, cancellationToken).ConfigureAwait(false);

        return await RunCoreAsync(agent, prompt, conversationId, additionalContext, resolveToolCalls, validateOutput, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<AgentResult> RunAsync(AgentReference agent, string prompt, string? conversationId = null,
        string? additionalContext = null,
        ToolCallResolver? resolveToolCalls = null,
        Func<AgentResult, string?>? validateOutput = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        cancellationToken.ThrowIfCancellationRequested();

        return await RunCoreAsync(agent, prompt, conversationId, additionalContext, resolveToolCalls, validateOutput, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<AgentResult> RunCoreAsync(AgentReference agent, string prompt, string? conversationId,
        string? additionalContext, ToolCallResolver? resolveToolCalls, Func<AgentResult, string?>? validateOutput,
        CancellationToken cancellationToken)
    {
        // Held for the whole run. Waiting for it doesn't count towards RunTimeout.
        await using var slot = runLimiter is null ? null : await runLimiter.AcquireAsync(cancellationToken).ConfigureAwait(false);
        using var telemetry = new AgentRunTelemetry(_runOptions.ApplicationName, agent);
        using var timeoutSource = _runOptions.RunTimeout is { } timeout ? CreateTimeoutSource(timeout, cancellationToken) : null;
        var runToken = timeoutSource?.Token ?? cancellationToken;

        // Added up per response, because every response is billed, even in a run that later fails.
        var usage = new UsageTally();
        string? createdConversationId = null;

        try
        {
            createdConversationId = conversationId is null
                ? await conversationClient.CreateConversationAsync(runToken).ConfigureAwait(false)
                : null;

            var conversation = conversationId ?? createdConversationId!;
            var response = await RunResponsesAsync(agent, conversation,
                BuildInitialInputItems(agent.Name, prompt, additionalContext), resolveToolCalls, usage, runToken).ConfigureAwait(false);
            telemetry.AnsweredBy(response.Model);

            // One retry in the same conversation: only the reason goes back, not the prompt and evidence.
            if (validateOutput?.Invoke(ToResult(agent, response, usage)) is { } problem)
            {
                _logger.LogWarning("Agent {AgentName} gave an invalid answer; asking once more: {Problem}", agent.Name, problem);
                response = await RunResponsesAsync(agent, conversation,
                    [ResponseItem.CreateUserMessageItem(string.Format(PromptText.AnswerRejected, problem))], resolveToolCalls, usage, runToken)
                    .ConfigureAwait(false);

                if (validateOutput(ToResult(agent, response, usage)) is { } stillInvalid)
                {
                    throw new InvalidOperationException(string.Format(ErrorMessages.AgentAnswerInvalid, agent.Name, stillInvalid));
                }
            }

            telemetry.Succeeded();
            return ToResult(agent, response, usage);
        }
        catch (OperationCanceledException ex) when (timeoutSource?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
        {
            telemetry.Failed("timeout", "timeout");
            _logger.LogError(ex, "Agent {AgentName} did not finish within {RunTimeout}", agent.Name, _runOptions.RunTimeout);
            throw WithUsage(new TimeoutException(string.Format(ErrorMessages.AgentRunTimedOut, agent.Name, _runOptions.RunTimeout), ex), usage);
        }
        catch (OperationCanceledException)
        {
            telemetry.Cancelled();
            throw;
        }
        catch (Exception ex)
        {
            telemetry.Failed("failure", ex.Message);
            _logger.LogError(ex, "Agent {AgentName} failed", agent.Name);
            throw WithUsage(new InvalidOperationException(string.Format(ErrorMessages.AgentRunFailed, agent.Name), ex), usage);
        }
        finally
        {
            telemetry.Record(usage.Total);
            if (createdConversationId is not null && _runOptions.DeleteConversationsAfterRun)
            {
                await DeleteConversationQuietlyAsync(agent.Name, createdConversationId).ConfigureAwait(false);
            }
        }
    }

    private static AgentResult ToResult(AgentReference agent, ResponseResult response, UsageTally usage)
        => new(agent.Name, response.GetOutputText(), usage.Total.TotalTokens)
        {
            InputTokens = usage.Total.InputTokens,
            OutputTokens = usage.Total.OutputTokens,
            AgentVersion = agent.Version,
            Model = response.Model,
        };

    private async Task<ResponseResult> RunResponsesAsync(AgentReference agent, string conversationId, IReadOnlyList<ResponseItem> inputItems,
        ToolCallResolver? resolveToolCalls,
        UsageTally usage, CancellationToken cancellationToken)
    {
        // Responses return synchronously, so no polling. Call again only for tool calls the model is waiting on.
        for (var round = 1; round <= MaxToolCallRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await conversationClient.CreateResponseAsync(agent.Name, conversationId, inputItems, agent.Version, cancellationToken)
                .ConfigureAwait(false);
            usage.Add(response.Usage);

            var toolCalls = response.OutputItems.OfType<FunctionCallResponseItem>().ToList();
            if (toolCalls.Count == 0)
            {
                ThrowIfNotCompleted(agent.Name, response);
                return response;
            }

            inputItems = await ResolveToolCallsAsync(response, toolCalls, resolveToolCalls, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException(string.Format(ErrorMessages.ToolCallRoundLimitExceeded, agent.Name, MaxToolCallRounds));
    }

    private async Task DeleteConversationQuietlyAsync(string agentName, string conversationId)
    {
        // Runs after cancellation too, so it gets its own short budget rather than the run's token.
        using var cleanupSource = new CancellationTokenSource(ConversationCleanupTimeout);
        try
        {
            await conversationClient.DeleteConversationAsync(conversationId, cleanupSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete conversation {ConversationId} for agent {AgentName}; it will be retained in Foundry.",
                conversationId, agentName);
        }
    }

    /// <summary>
    /// Attaches the tokens a failed run used to its exception, so a fallback result (and the briefing
    /// total built from it) still reports what Foundry billed. Read with <see cref="AgentTelemetry.TokenUsageOf"/>.
    /// </summary>
    private static Exception WithUsage(Exception exception, UsageTally usage)
    {
        exception.Data[AgentTelemetry.TokenUsageDataKey] = usage.Total;
        return exception;
    }

    /// <summary>Adds up the usage of every response in one run.</summary>
    private sealed class UsageTally
    {
        public TokenUsage Total { get; private set; } = TokenUsage.None;

        public void Add(ResponseTokenUsage? usage)
        {
            if (usage is not null)
            {
                Total += new TokenUsage(usage.InputTokenCount, usage.OutputTokenCount, usage.TotalTokenCount);
            }
        }
    }

    private static CancellationTokenSource CreateTimeoutSource(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(timeout);
        return source;
    }

    private IReadOnlyList<ResponseItem> BuildInitialInputItems(string agentName, string prompt, string? additionalContext)
        => string.IsNullOrWhiteSpace(additionalContext)
            ? [ResponseItem.CreateUserMessageItem(prompt)]
            : [ResponseItem.CreateUserMessageItem(PromptText.FenceReferenceMaterial(LimitEvidence(agentName, additionalContext))),
               ResponseItem.CreateUserMessageItem(prompt)];

    /// <summary>
    /// Keeps evidence within <see cref="AgentRunOptions.MaxEvidenceCharacters"/>, keeping the start (the most relevant
    /// results), so oversized evidence can't exceed the model's context or run up the bill. Cut before fencing.
    /// </summary>
    private string LimitEvidence(string agentName, string evidence)
    {
        var limit = _runOptions.MaxEvidenceCharacters;
        if (evidence.Length <= limit)
        {
            return evidence;
        }

        _logger.LogWarning("Evidence for agent {AgentName} was {Length} characters; truncated to {Limit}", agentName, evidence.Length, limit);
        return evidence[..limit] + string.Format(PromptText.EvidenceTruncated, evidence.Length - limit);
    }

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

    private async Task<IReadOnlyList<ResponseItem>> ResolveToolCallsAsync(ResponseResult response,
        IReadOnlyList<FunctionCallResponseItem> toolCalls,
        ToolCallResolver? resolveToolCalls,
        CancellationToken cancellationToken)
    {
        if (resolveToolCalls is null)
        {
            throw new InvalidOperationException(string.Format(ErrorMessages.ToolCallsRequiredNoCallback, response.Id));
        }

        var requests = toolCalls
            .Select(call => new ToolCallRequest(call.CallId, call.FunctionName, call.FunctionArguments.ToString()))
            .ToList();
        var outputs = (await resolveToolCalls(requests, cancellationToken).ConfigureAwait(false)).ToList();

        var duplicate = outputs.GroupBy(o => o.CallId).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(string.Format(ErrorMessages.DuplicateToolCallOutput, duplicate.Key));
        }

        var outputByCallId = outputs.ToDictionary(o => o.CallId, o => o.Output);

        var nextInputItems = new List<ResponseItem>(response.OutputItems);
        foreach (var callId in toolCalls.Select(toolCall => toolCall.CallId))
        {
            if (!outputByCallId.TryGetValue(callId, out var output))
            {
                throw new InvalidOperationException(string.Format(ErrorMessages.MissingToolCallOutput, callId));
            }

            var limited = LimitToolOutput(callId, output);
            nextInputItems.Add(ResponseItem.CreateFunctionCallOutputItem(callId,
                _runOptions.FenceToolOutput ? PromptText.FenceToolOutput(limited) : limited));
        }

        return nextInputItems;
    }

    /// <summary>
    /// Keeps a tool's output within <see cref="AgentRunOptions.MaxToolOutputCharacters"/>, so one large
    /// result can't flood the model's context (and the bill) or carry an unbounded injection payload.
    /// </summary>
    private string LimitToolOutput(string callId, string output)
    {
        var limit = _runOptions.MaxToolOutputCharacters;
        if (output.Length <= limit)
        {
            return output;
        }

        _logger.LogWarning("Tool output for call {CallId} was {Length} characters; truncated to {Limit}", callId, output.Length, limit);
        return output[..limit] + string.Format(PromptText.ToolOutputTruncated, output.Length - limit);
    }
}
