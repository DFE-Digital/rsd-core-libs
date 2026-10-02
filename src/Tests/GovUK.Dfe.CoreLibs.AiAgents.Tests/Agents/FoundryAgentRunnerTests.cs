using GovUK.Dfe.CoreLibs.AiAgents.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Extensions;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using NSubstitute;
using OpenAI.Responses;
using System.ClientModel.Primitives;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Agents;

public sealed class FoundryAgentRunnerTests
{
    private readonly CancellationToken cancellationToken = default;
    private readonly IAgentFactory _agentFactory = Substitute.For<IAgentFactory>();
    private readonly IFoundryConversationClient _conversationClient = Substitute.For<IFoundryConversationClient>();

    private FoundryAgentRunner CreateSut() => new(_agentFactory, _conversationClient);
     
    private static ResponseResult ResponseWithOutputItems(string id, IEnumerable<ResponseItem> items, int? totalTokens = null, string status = "completed",
        string? incompleteReason = null)
    {
        var itemsJson = string.Join(',', items.Select(item => ModelReaderWriter.Write(item).ToString()));
        var usageJson = totalTokens is null
            ? ""
            : $",\"usage\":{{\"input_tokens\":10,\"output_tokens\":{totalTokens - 10},\"total_tokens\":{totalTokens}}}";
        var incompleteJson = incompleteReason is null ? "" : $",\"incomplete_details\":{{\"reason\":\"{incompleteReason}\"}}";
        var json = $"{{\"id\":\"{id}\",\"object\":\"response\",\"created_at\":0,\"status\":\"{status}\",\"model\":\"gpt-4o\",\"output\":[{itemsJson}]{usageJson}{incompleteJson}}}";
        return ModelReaderWriter.Read<ResponseResult>(BinaryData.FromString(json))!;
    }

    private static ResponseResult CompletedResponse(string id, string text, int totalTokens = 30)
        => ResponseWithOutputItems(id, [ResponseItem.CreateAssistantMessageItem(text, (IEnumerable<ResponseMessageAnnotation>?)null)], totalTokens);

    private static ResponseResult ResponseWithFunctionCall(string id, string callId, string functionName)
        => ResponseWithOutputItems(id,
            [ResponseItem.CreateFunctionCallItem(callId: callId, functionName: functionName, functionArguments: BinaryData.FromString("{}"))]);

    [Fact]
    public async Task RunAsync_Throws_WhenToolCallsHaveNoResolver()
    {
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };
        _agentFactory.GetOrCreateAsync(spec, cancellationToken).Returns(new AgentReference("agent-id", "my-agent"));
        _conversationClient.CreateConversationAsync(cancellationToken).Returns("conversation-1");
        _conversationClient.CreateResponseAsync("my-agent", "conversation-1", Arg.Any<IReadOnlyList<ResponseItem>>(), Arg.Any<string?>(), Arg.Any<int?>(), cancellationToken)
            .Returns(ResponseWithFunctionCall("resp-1", "call-1", "lookup"));

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunAsync(spec, "prompt", cancellationToken: cancellationToken));
    }

    [Theory]
    [InlineData("failed", null, "resp-1 did not complete (status 'Failed')")]
    [InlineData("incomplete", "max_output_tokens", "used its 32000 output tokens for this run. Raise MaxOutputTokensPerRun")]
    public async Task RunAsync_Throws_WhenResponseDoesNotComplete_OrTheModelStopsAtTheOutputTokenCap(string status, string? reason, string expected)
    {
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };
        _agentFactory.GetOrCreateAsync(spec, cancellationToken).Returns(new AgentReference("agent-id", "my-agent"));
        _conversationClient.CreateConversationAsync(cancellationToken).Returns("conversation-1");
        _conversationClient.CreateResponseAsync("my-agent", "conversation-1", Arg.Any<IReadOnlyList<ResponseItem>>(), Arg.Any<string?>(), Arg.Any<int?>(), cancellationToken)
            .Returns(ResponseWithOutputItems("resp-1", [], status: status, incompleteReason: reason));

        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunAsync(spec, "prompt", cancellationToken: cancellationToken));
        var inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Contains(expected, inner.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(32_000, 10, "exceeded the maximum of 10 tool-call rounds")]   // each round uses 90 output tokens
    [InlineData(100, 1, "used its 100 output tokens for this run")]         // 10 left after one round: too few to go on
    public async Task RunAsync_StopsARunawayRun_AfterTenToolRounds_OrOnceItsOutputTokensRunOut(int maxOutputTokensPerRun, int expectedCalls,
        string expected)
    {
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };
        _agentFactory.GetOrCreateAsync(spec, cancellationToken).Returns(new AgentReference("agent-id", "my-agent"));
        _conversationClient.CreateConversationAsync(cancellationToken).Returns("conversation-1");
        _conversationClient.CreateResponseAsync("my-agent", "conversation-1", Arg.Any<IReadOnlyList<ResponseItem>>(), Arg.Any<string?>(), Arg.Any<int?>(), cancellationToken)
            .Returns(_ => ResponseWithOutputItems("resp-loop", [ResponseItem.CreateFunctionCallItem("call-1", "lookup", BinaryData.FromString("{}"))],
                totalTokens: 100));

        var sut = new FoundryAgentRunner(_agentFactory, _conversationClient,
            runOptions: new AgentRunOptions { MaxOutputTokensPerRun = maxOutputTokensPerRun });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunAsync(spec, "prompt", cancellationToken: cancellationToken,
            resolveToolCalls: (calls, _) => Task.FromResult<IEnumerable<ToolCallOutput>>(
                calls.Select(c => new ToolCallOutput(c.CallId, "42")))));

        Assert.Contains(expected, ex.InnerException!.Message, StringComparison.Ordinal);
        await _conversationClient.Received(expectedCalls).CreateResponseAsync("my-agent", "conversation-1", Arg.Any<IReadOnlyList<ResponseItem>>(), Arg.Any<string?>(), Arg.Any<int?>(), cancellationToken);
    }

    [Fact]
    public async Task RunAsync_LogsAndRethrows_WhenConversationClientThrows()
    {
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };
        _agentFactory.GetOrCreateAsync(spec, cancellationToken).Returns(new AgentReference("agent-id", "my-agent"));
        _conversationClient.CreateConversationAsync(cancellationToken).Returns("conversation-1");
        _conversationClient.CreateResponseAsync("my-agent", "conversation-1", Arg.Any<IReadOnlyList<ResponseItem>>(), Arg.Any<string?>(), Arg.Any<int?>(), cancellationToken)
            .Returns(Task.FromException<ResponseResult>(new InvalidOperationException("boom")));

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunAsync(spec, "prompt", cancellationToken: cancellationToken));
    }

    [Fact]
    public async Task RunAsync_ByReference_ThrowsArgumentException_WhenPromptIsEmpty()
    {
        var sut = CreateSut();
        var agent = new AgentReference("agent-id", "my-agent", "1");

        await Assert.ThrowsAsync<ArgumentException>(() => sut.RunAsync(agent, "  ", cancellationToken: cancellationToken));
    }

    [Fact]
    public async Task RunAsync_ByReference_ReusesGivenConversation_WithoutCreatingANewOne()
    {
        var agent = new AgentReference("agent-id", "my-agent", "1");
        _conversationClient.CreateResponseAsync("my-agent", "existing-conversation", Arg.Any<IReadOnlyList<ResponseItem>>(), "1", Arg.Any<int?>(), cancellationToken)
            .Returns(CompletedResponse("resp-1", "Reply."));

        var sut = CreateSut();

        await sut.RunAsync(agent, "prompt", conversationId: "existing-conversation", cancellationToken: cancellationToken);

        await _conversationClient.DidNotReceiveWithAnyArgs().CreateConversationAsync(cancellationToken);
    }

    [Fact]
    public async Task RunAsync_CutsEvidenceToMaxEvidenceCharacters_KeepingTheStart_InsideTheFence()
    {
        var agent = new AgentReference("agent-id", "my-agent", "2");
        _conversationClient.CreateConversationAsync(cancellationToken).Returns("conversation-1");
        var capturedInput = new List<ResponseItem>();
        _conversationClient.CreateResponseAsync("my-agent", "conversation-1",
                Arg.Do<IReadOnlyList<ResponseItem>>(items => capturedInput.AddRange(items)), "2", Arg.Any<int?>(), cancellationToken)
            .Returns(CompletedResponse("resp-1", "Answer."));
        var sut = new FoundryAgentRunner(_agentFactory, _conversationClient, runOptions: new AgentRunOptions { MaxEvidenceCharacters = 10 });

        await sut.RunAsync(agent, "Summarise.", additionalContext: "MOST-RELEVANT" + new string('x', 500), cancellationToken: cancellationToken);

        var fenced = ModelReaderWriter.Write(capturedInput[0]).ToString();
        Assert.Contains("MOST-RELEV", fenced, StringComparison.Ordinal);
        Assert.DoesNotContain("xxxxx", fenced, StringComparison.Ordinal);
        Assert.Contains("Evidence truncated: 503 more characters", fenced, StringComparison.Ordinal);
        Assert.Matches("END_REFERENCE_MATERIAL [0-9A-F]+>>>", fenced);
    }

    [Fact]
    public async Task RunAsync_AddsUpTokens_AcrossEveryToolCallRound_AndCapsEachResponseAtTheOutputTokensLeft()
    {
        var agent = new AgentReference("agent-id", "my-agent", "1");
        var caps = new List<int?>();
        _conversationClient.CreateConversationAsync(Arg.Any<CancellationToken>()).Returns("conversation-1");
        _conversationClient.CreateResponseAsync("my-agent", "conversation-1", Arg.Any<IReadOnlyList<ResponseItem>>(), "1", Arg.Do<int?>(caps.Add), Arg.Any<CancellationToken>())
            .Returns(
                ResponseWithOutputItems("resp-1", [ResponseItem.CreateFunctionCallItem("call-1", "lookup", BinaryData.FromString("{}"))], totalTokens: 100),
                CompletedResponse("resp-2", "Answer.", totalTokens: 30));

        var result = await CreateSut().RunAsync(agent, "prompt",
            resolveToolCalls: (_, _) => Task.FromResult<IEnumerable<ToolCallOutput>>([new ToolCallOutput("call-1", "42")]),
            cancellationToken: cancellationToken);

        Assert.Equal(130, result.TotalTokens);
        Assert.Equal(20, result.InputTokens);
        Assert.Equal(110, result.OutputTokens);
        Assert.Equal([32_000, 31_910], caps);   // the first round used 90
    }

    [Fact]
    public async Task RunAsync_AttachesTheTokensUsedBeforeAFailure_ToTheException()
    {
        var agent = new AgentReference("agent-id", "my-agent", "1");
        _conversationClient.CreateConversationAsync(Arg.Any<CancellationToken>()).Returns("conversation-1");
        _conversationClient.CreateResponseAsync("my-agent", "conversation-1", Arg.Any<IReadOnlyList<ResponseItem>>(), "1", Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(ResponseWithOutputItems("resp-1", [ResponseItem.CreateFunctionCallItem("call-1", "lookup", BinaryData.FromString("{}"))], totalTokens: 100));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut().RunAsync(agent, "prompt",
            resolveToolCalls: (_, _) => throw new HttpRequestException("Tool server down."), cancellationToken: cancellationToken));

        Assert.Equal(new TokenUsage(10, 90, 100), GovUK.Dfe.CoreLibs.AiAgents.Diagnostics.AgentTelemetry.TokenUsageOf(ex));
        Assert.Equal(100, new AgentStepResult("my-agent", null, ex).ToAgentResult().TotalTokens);
    }

    [Fact]
    public async Task RunAsync_TruncatesToolOutput_ToTheConfiguredLimit()
    {
        var agent = new AgentReference("agent-id", "my-agent", "1");
        var captured = new List<IReadOnlyList<ResponseItem>>();
        _conversationClient.CreateConversationAsync(Arg.Any<CancellationToken>()).Returns("conversation-1");
        _conversationClient.CreateResponseAsync("my-agent", "conversation-1", Arg.Do<IReadOnlyList<ResponseItem>>(captured.Add), "1", Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(ResponseWithFunctionCall("resp-1", "call-1", "lookup"), CompletedResponse("resp-2", "Answer."));
        var sut = new FoundryAgentRunner(_agentFactory, _conversationClient, runOptions: new AgentRunOptions { MaxToolOutputCharacters = 50 });

        await sut.RunAsync(agent, "prompt",
            resolveToolCalls: (_, _) => Task.FromResult<IEnumerable<ToolCallOutput>>([new ToolCallOutput("call-1", new string('x', 500))]),
            cancellationToken: cancellationToken);

        var followUp = string.Join(' ', captured[1].Select(item => ModelReaderWriter.Write(item).ToString()));
        Assert.Contains(new string('x', 50), followUp, StringComparison.Ordinal);
        Assert.DoesNotContain(new string('x', 51), followUp, StringComparison.Ordinal);
        Assert.Contains("450 more characters were not included", followUp, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_NeverDeletesAConversationTheCallerPassedIn()
    {
        var agent = new AgentReference("agent-id", "my-agent", "1");
        _conversationClient.CreateResponseAsync("my-agent", "callers-conversation", Arg.Any<IReadOnlyList<ResponseItem>>(), "1", Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(CompletedResponse("resp-1", "Answer."));

        await CreateSut().RunAsync(agent, "prompt", conversationId: "callers-conversation", cancellationToken: cancellationToken);

        await _conversationClient.DidNotReceiveWithAnyArgs().DeleteConversationAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task RunAsync_Throws_WhenTheCallbackReturnsTwoOutputsForOneCall()
    {
        var agent = new AgentReference("agent-id", "my-agent", "1");
        _conversationClient.CreateConversationAsync(Arg.Any<CancellationToken>()).Returns("conversation-1");
        _conversationClient.CreateResponseAsync("my-agent", "conversation-1", Arg.Any<IReadOnlyList<ResponseItem>>(), "1", Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(ResponseWithFunctionCall("resp-1", "call-1", "lookup"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut().RunAsync(agent, "prompt",
            resolveToolCalls: (_, _) => Task.FromResult<IEnumerable<ToolCallOutput>>([new("call-1", "a"), new("call-1", "b")]),
            cancellationToken: cancellationToken));

        Assert.Contains("More than one tool output", ex.InnerException!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_ThrowsTimeoutException_WhenTheRunTimeoutElapses()
    {
        var agent = new AgentReference("agent-id", "my-agent", "1");
        _conversationClient.CreateConversationAsync(Arg.Any<CancellationToken>()).Returns("conversation-1");
        _conversationClient.CreateResponseAsync("my-agent", "conversation-1", Arg.Any<IReadOnlyList<ResponseItem>>(), "1", Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(Timeout.Infinite, callInfo.Arg<CancellationToken>());
                return CompletedResponse("resp-1", "never");
            });
        var sut = new FoundryAgentRunner(_agentFactory, _conversationClient, runOptions: new AgentRunOptions { RunTimeout = TimeSpan.FromMilliseconds(100) });

        var ex = await Assert.ThrowsAsync<TimeoutException>(() => sut.RunAsync(agent, "prompt", cancellationToken: cancellationToken));

        Assert.Contains("my-agent", ex.Message, StringComparison.Ordinal);
        await _conversationClient.Received(1).DeleteConversationAsync("conversation-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_PropagatesCallerCancellation_AsCancellation_NotAsATimeout()
    {
        var agent = new AgentReference("agent-id", "my-agent", "1");
        using var caller = new CancellationTokenSource();
        _conversationClient.CreateConversationAsync(Arg.Any<CancellationToken>()).Returns("conversation-1");
        _conversationClient.CreateResponseAsync("my-agent", "conversation-1", Arg.Any<IReadOnlyList<ResponseItem>>(), "1", Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await caller.CancelAsync();
                await Task.Delay(Timeout.Infinite, callInfo.Arg<CancellationToken>());
                return CompletedResponse("resp-1", "never");
            });
        var sut = new FoundryAgentRunner(_agentFactory, _conversationClient, runOptions: new AgentRunOptions { RunTimeout = TimeSpan.FromMinutes(5) });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(agent, "prompt", cancellationToken: caller.Token));

        await _conversationClient.Received(1).DeleteConversationAsync("conversation-1", Arg.Any<CancellationToken>());
    }
}
