using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using OpenAI.Responses;
using System.Collections.Concurrent;
using System.ClientModel.Primitives;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration.Fakes;

/// <summary>A response call as Foundry would have received it.</summary>
internal sealed record RecordedResponseCall(string AgentName, string? AgentVersion, string ConversationId, string SerializedInput,
    int? MaxOutputTokens = null);

/// <summary>
/// Plays back scripted Responses per agent name and records every call. Ephemeral agents are
/// matched by prefix, since the runtime suffixes their names with a GUID.
/// </summary>
internal sealed class ScriptedConversationClient : IFoundryConversationClient
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<Func<CancellationToken, Task<ResponseResult>>>> _scripts = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<RecordedResponseCall> _calls = new();
    private readonly ConcurrentDictionary<string, bool> _openConversations = new(StringComparer.Ordinal);
    private int _conversationCount;
    private int _inFlight;
    private int _peakInFlight;

    public IReadOnlyList<RecordedResponseCall> Calls => [.. _calls];

    public int ConversationsCreated => _conversationCount;

    /// <summary>Conversations created and not yet deleted - i.e. what Foundry would still be retaining.</summary>
    public IReadOnlyCollection<string> OpenConversations => [.. _openConversations.Keys];

    /// <summary>The most responses that were ever being generated at the same time.</summary>
    public int PeakConcurrentResponses => Volatile.Read(ref _peakInFlight);

    /// <summary>When set, deletes fail - to exercise clean-up failure handling.</summary>
    public bool FailDeletes { get; set; }

    public IReadOnlyList<RecordedResponseCall> CallsFor(string agentName)
        => [.. _calls.Where(call => Matches(agentName, call.AgentName))];

    public void Reply(string agentName, params ResponseResult[] responses)
    {
        foreach (var response in responses)
        {
            Script(agentName).Enqueue(_ => Task.FromResult(response));
        }
    }

    public void ReplyAfter(string agentName, TimeSpan delay, ResponseResult response)
        => Script(agentName).Enqueue(async ct =>
        {
            await Task.Delay(delay, ct);
            return response;
        });

    /// <summary>Never answers; only the caller's cancellation (or a run timeout) ends the call.</summary>
    public void Hang(string agentName)
        => Script(agentName).Enqueue(async ct =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("Unreachable.");
        });

    public void Fail(string agentName, Exception exception)
        => Script(agentName).Enqueue(_ => Task.FromException<ResponseResult>(exception));

    public Task<string> CreateConversationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = $"conversation-{Interlocked.Increment(ref _conversationCount)}";
        _openConversations[id] = true;
        return Task.FromResult(id);
    }

    public Task DeleteConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        if (FailDeletes)
        {
            return Task.FromException(new InvalidOperationException("Conversation delete failed."));
        }

        _openConversations.TryRemove(conversationId, out _);
        return Task.CompletedTask;
    }

    public async Task<ResponseResult> CreateResponseAsync(string agentName, string conversationId, IReadOnlyList<ResponseItem> inputItems,
        string? agentVersion = null, int? maxOutputTokens = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var input = string.Join(' ', inputItems.Select(item => ModelReaderWriter.Write(item).ToString()));
        _calls.Enqueue(new RecordedResponseCall(agentName, agentVersion, conversationId, input, maxOutputTokens));

        var script = _scripts.FirstOrDefault(pair => Matches(pair.Key, agentName)).Value
            ?? throw new InvalidOperationException($"No scripted response for agent '{agentName}'.");

        var next = NextStep(script) ?? throw new InvalidOperationException($"Script for agent '{agentName}' is empty.");

        var now = Interlocked.Increment(ref _inFlight);
        UpdatePeak(now);
        try
        {
            return await next(cancellationToken);
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    private ConcurrentQueue<Func<CancellationToken, Task<ResponseResult>>> Script(string agentName)
        => _scripts.GetOrAdd(agentName, _ => new ConcurrentQueue<Func<CancellationToken, Task<ResponseResult>>>());

    /// <summary>The last scripted step repeats, so a test only scripts what differs between calls.</summary>
    private static Func<CancellationToken, Task<ResponseResult>>? NextStep(ConcurrentQueue<Func<CancellationToken, Task<ResponseResult>>> script)
    {
        if (script.Count > 1 && script.TryDequeue(out var dequeued))
        {
            return dequeued;
        }

        return script.TryPeek(out var last) ? last : null;
    }

    private void UpdatePeak(int value)
    {
        var current = Volatile.Read(ref _peakInFlight);
        while (current < value && Interlocked.CompareExchange(ref _peakInFlight, value, current) != current)
        {
            current = Volatile.Read(ref _peakInFlight);
        }
    }

    private static bool Matches(string scriptedName, string actualName)
        => actualName == scriptedName
           || (actualName.StartsWith(scriptedName + "-", StringComparison.Ordinal)
               && Guid.TryParseExact(actualName[(scriptedName.Length + 1)..], "N", out _));
}

internal static class FoundryResponses
{
    public static ResponseResult Completed(string id, string text, int totalTokens = 30)
        => WithOutputItems(id, [ResponseItem.CreateAssistantMessageItem(text, (IEnumerable<ResponseMessageAnnotation>?)null)], totalTokens);

    public static ResponseResult FunctionCall(string id, string callId, string functionName)
        => WithOutputItems(id, [ResponseItem.CreateFunctionCallItem(callId: callId, functionName: functionName,
            functionArguments: BinaryData.FromString("{}"))]);

    /// <summary>Usage always reports 10 input tokens and the rest as output tokens.</summary>
    public static ResponseResult WithOutputItems(string id, IEnumerable<ResponseItem> items, int? totalTokens = null, string status = "completed")
    {
        var itemsJson = string.Join(',', items.Select(item => ModelReaderWriter.Write(item).ToString()));
        var usageJson = totalTokens is null
            ? ""
            : $",\"usage\":{{\"input_tokens\":10,\"output_tokens\":{totalTokens - 10},\"total_tokens\":{totalTokens}}}";
        var json = $"{{\"id\":\"{id}\",\"object\":\"response\",\"created_at\":0,\"status\":\"{status}\",\"model\":\"gpt-4o\",\"output\":[{itemsJson}]{usageJson}}}";
        return ModelReaderWriter.Read<ResponseResult>(BinaryData.FromString(json))!;
    }
}
