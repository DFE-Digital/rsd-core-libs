using Azure.AI.Projects.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration;
using NSubstitute;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;
using Xunit;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration;
 
public sealed class FoundryOrchestrationIntegrationTests
{
    private const string ConversationId = "conversation-1";

    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;
    private readonly AgentAdministrationClient _admin = Substitute.For<AgentAdministrationClient>();
    private readonly IFoundryConversationClient _conversationClient = Substitute.For<IFoundryConversationClient>();
    private readonly FoundryAgentFactoryOptions _options = new("gpt-4o");
    private FoundryAgentFactory? _factory;

    private AgentOrchestrator CreateSut()
    {
        _conversationClient.CreateConversationAsync(Arg.Any<CancellationToken>()).Returns(ConversationId);

        _factory = new FoundryAgentFactory(_admin, _options);
        var runner = new FoundryAgentRunner(_factory, _conversationClient);
        return new AgentOrchestrator(runner);
    }

    private static AgentSpec Spec(string name) => new() { Name = name, Instructions = $"You are {name}." };
     
    private Task<AgentReference> ProvisionAsync(string name) => _factory!.GetOrCreateAsync(Spec(name), cancellationToken);

    private static AgentOrchestrationStep StepFor(AgentReference agent, Func<CancellationToken, Task<string>> resolvePrompt)
        => new(agent.Name, _ => Task.FromResult(agent), resolvePrompt);

    private void SetUpNewAgent(string name)
    {
        var notFound = NotFoundException();
        _admin.GetAgentAsync(name, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ClientResult<ProjectsAgentRecord>>(notFound));

        var version = ProjectsAgentsModelFactory.ProjectsAgentVersion(id: $"{name}-v1", name: name, version: "1");
        _admin.CreateAgentVersionAsync(name, Arg.Any<ProjectsAgentVersionCreationOptions>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ClientResult.FromValue(version, FakeResponse())));
    }

    private static PipelineResponse FakeResponse() => Substitute.For<PipelineResponse>();

    private static ClientResultException NotFoundException()
    {
        var response = FakeResponse();
        response.Status.Returns(404);
        return new ClientResultException(response, null!);
    }

    private void SetUpAgentResponse(string agentName, params ResponseResult[] responses)
    {
        SetUpNewAgent(agentName);
        _conversationClient.CreateResponseAsync(agentName, ConversationId, Arg.Any<IReadOnlyList<ResponseItem>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(responses[0], responses[1..]);
    }

    private void SetUpAgentFailure(string agentName, Exception exception)
    {
        SetUpNewAgent(agentName);
        _conversationClient.CreateResponseAsync(agentName, ConversationId, Arg.Any<IReadOnlyList<ResponseItem>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ResponseResult>(exception));
    }

    private List<ResponseItem> CapturePromptFor(string agentName, ResponseResult response)
    {
        var captured = new List<ResponseItem>();
        SetUpNewAgent(agentName);
        _conversationClient.CreateResponseAsync(agentName, ConversationId,
                Arg.Do<IReadOnlyList<ResponseItem>>(items => captured.AddRange(items)), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(response);
        return captured;
    }

    private static string SerializeInput(IReadOnlyList<ResponseItem> items)
        => string.Join(' ', items.Select(item => ModelReaderWriter.Write(item).ToString()));
     
    private static ResponseResult ResponseWithOutputItems(string id, IEnumerable<ResponseItem> items, int? totalTokens = null, string status = "completed")
    {
        var itemsJson = string.Join(',', items.Select(item => ModelReaderWriter.Write(item).ToString()));
        var usageJson = totalTokens is null
            ? ""
            : $",\"usage\":{{\"input_tokens\":10,\"output_tokens\":{totalTokens - 10},\"total_tokens\":{totalTokens}}}";
        var json = $"{{\"id\":\"{id}\",\"object\":\"response\",\"created_at\":0,\"status\":\"{status}\",\"model\":\"gpt-4o\",\"output\":[{itemsJson}]{usageJson}}}";
        return ModelReaderWriter.Read<ResponseResult>(BinaryData.FromString(json))!;
    }

    private static ResponseResult CompletedResponse(string id, string text, int totalTokens = 30)
        => ResponseWithOutputItems(id, [ResponseItem.CreateAssistantMessageItem(text, (IEnumerable<ResponseMessageAnnotation>?)null)], totalTokens);

    private static ResponseResult ResponseWithFunctionCall(string id, string callId, string functionName)
        => ResponseWithOutputItems(id,
            [ResponseItem.CreateFunctionCallItem(callId: callId, functionName: functionName, functionArguments: BinaryData.FromString("{}"))]);

    // ===================== Sequential =====================

    [Fact]
    public async Task RunSequentialAsync_ChainsRealAgentCreationExecutionAndOutputPassing()
    {
        SetUpAgentResponse("researcher", CompletedResponse("r1", "Fact: sky is blue.", totalTokens: 20));
        var writerPrompt = CapturePromptFor("writer", CompletedResponse("r2", "Report: the sky is blue.", totalTokens: 15));

        var sut = CreateSut();
        var context = new AgentContext();
        var agents = new[] { await ProvisionAsync("researcher"), await ProvisionAsync("writer") };

        var result = await sut.RunSequentialAsync(agents, "Research the sky.", context,
            cancellationToken: cancellationToken);

        Assert.Equal("Report: the sky is blue.", result.FinalOutput);
        Assert.Equal(2, result.Results.Count);
        Assert.All(result.Results, r => Assert.True(r.Succeeded));

        // The second agent's prompt genuinely carries the first agent's real output forward.
        Assert.Contains("Fact: sky is blue.", SerializeInput(writerPrompt), StringComparison.Ordinal);

        // Real agent creation happened exactly once per distinct agent name.
        await _admin.Received(1).CreateAgentVersionAsync("researcher", Arg.Any<ProjectsAgentVersionCreationOptions>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _admin.Received(1).CreateAgentVersionAsync("writer", Arg.Any<ProjectsAgentVersionCreationOptions>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        Assert.Equal(2, context.History.Count);
        Assert.Equal("researcher", context.History[0].AgentName);
        Assert.Equal("writer", context.History[1].AgentName);
    }

    [Fact]
    public async Task RunSequentialAsync_ContinuesWithLastSuccessfulOutput_WhenAMiddleStepFails()
    {
        SetUpAgentResponse("step1", CompletedResponse("s1", "output-1"));
        SetUpAgentFailure("step2", new InvalidOperationException("boom"));
        var step3Prompt = CapturePromptFor("step3", CompletedResponse("s3", "output-3"));

        var sut = CreateSut();
        var context = new AgentContext();
        var agents = new[] { await ProvisionAsync("step1"), await ProvisionAsync("step2"), await ProvisionAsync("step3") };

        var result = await sut.RunSequentialAsync(agents, "initial", context,
            cancellationToken: cancellationToken);

        Assert.Equal(3, result.Results.Count);
        Assert.True(result.Results[0].Succeeded);
        Assert.False(result.Results[1].Succeeded);
        Assert.IsType<InvalidOperationException>(result.Results[1].Error);
        Assert.True(result.Results[2].Succeeded);

        // step2 failed, so step3 should still have received step1's output, not step2's (nonexistent) output.
        Assert.Contains("output-1", SerializeInput(step3Prompt), StringComparison.Ordinal);
        Assert.Equal("output-3", result.FinalOutput);
    }

    [Fact]
    public async Task RunSequentialAsync_PropagatesFailure_WhenShouldSuppressReturnsFalse()
    {
        var failure = new InvalidOperationException("mandatory step failed");
        SetUpAgentFailure("mandatory", failure);
        SetUpNewAgent("never-reached");

        var sut = CreateSut();
        var context = new AgentContext();
        var agents = new[] { await ProvisionAsync("mandatory"), await ProvisionAsync("never-reached") };

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunSequentialAsync(
            agents, "initial", context, shouldSuppress: _ => false, cancellationToken: cancellationToken));

        Assert.Same(failure, thrown);
        await _conversationClient.DidNotReceive().CreateResponseAsync("never-reached", Arg.Any<string>(), Arg.Any<IReadOnlyList<ResponseItem>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    // ===================== Parallel =====================

    [Fact]
    public async Task RunParallelAsync_SharedInput_CombinesOutputsInAgentOrder()
    {
        SetUpAgentResponse("agentA", CompletedResponse("a1", "output-A"));
        SetUpAgentResponse("agentB", CompletedResponse("b1", "output-B"));
        SetUpAgentResponse("agentC", CompletedResponse("c1", "output-C"));

        var sut = CreateSut();
        var context = new AgentContext();
        var agents = new[] { await ProvisionAsync("agentA"), await ProvisionAsync("agentB"), await ProvisionAsync("agentC") };

        var result = await sut.RunParallelAsync(agents, "shared input", context,
            cancellationToken: cancellationToken);

        Assert.Equal($"output-A{Environment.NewLine}{Environment.NewLine}output-B{Environment.NewLine}{Environment.NewLine}output-C", result.FinalOutput);
        Assert.Equal(3, result.Results.Count);
    }

    [Fact]
    public async Task RunParallelAsync_ExecutesAgentsConcurrently_NotOneAtATime()
    {
        SetUpNewAgent("agentA");
        SetUpNewAgent("agentB");
        var gateA = new TaskCompletionSource<ResponseResult>();
        var gateB = new TaskCompletionSource<ResponseResult>();

        _conversationClient.CreateResponseAsync("agentA", ConversationId, Arg.Any<IReadOnlyList<ResponseItem>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => gateA.Task);
        _conversationClient.CreateResponseAsync("agentB", ConversationId, Arg.Any<IReadOnlyList<ResponseItem>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => gateB.Task);

        var sut = CreateSut();
        var context = new AgentContext();
        var agents = new[] { await ProvisionAsync("agentA"), await ProvisionAsync("agentB") };

        var runTask = sut.RunParallelAsync(agents, "input", context, cancellationToken: cancellationToken);

        await Task.Delay(50, cancellationToken);

        // Both requests must already be in flight - if execution were sequential, agentB would
        // never have been called while agentA's gate is still open.
        await _conversationClient.Received(1).CreateResponseAsync("agentB", ConversationId, Arg.Any<IReadOnlyList<ResponseItem>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());

        gateA.SetResult(CompletedResponse("a1", "output-A"));
        gateB.SetResult(CompletedResponse("b1", "output-B"));
        var result = await runTask;

        Assert.Equal(2, result.Results.Count);
        Assert.All(result.Results, r => Assert.True(r.Succeeded));
    }

    [Fact]
    public async Task RunParallelAsync_IndependentSteps_EachAgentGetsOnlyItsOwnResolvedPrompt()
    {
        var ofstedPrompt = CapturePromptFor("ofsted", CompletedResponse("o1", "Ofsted findings."));
        var trustPrompt = CapturePromptFor("trust", CompletedResponse("t1", "Trust findings."));

        var sut = CreateSut();
        var ofsted = await ProvisionAsync("ofsted");
        var trust = await ProvisionAsync("trust");

        var steps = new List<AgentOrchestrationStep>
        {
            StepFor(ofsted, _ => Task.FromResult("Evidence scoped to Ofsted only.")),
            StepFor(trust, _ => Task.FromResult("Evidence scoped to Trust only.")),
        };

        var context = new AgentContext();

        await sut.RunParallelAsync(steps, context, cancellationToken: cancellationToken);

        Assert.Contains("Evidence scoped to Ofsted only.", SerializeInput(ofstedPrompt), StringComparison.Ordinal);
        Assert.DoesNotContain("Trust only", SerializeInput(ofstedPrompt), StringComparison.Ordinal);
        Assert.Contains("Evidence scoped to Trust only.", SerializeInput(trustPrompt), StringComparison.Ordinal);
        Assert.DoesNotContain("Ofsted only", SerializeInput(trustPrompt), StringComparison.Ordinal); 
        Assert.Empty(context.History);
    }

    [Fact]
    public async Task RunParallelAsync_MandatoryStep_PropagatesFailure_WhenShouldSuppressReturnsFalse()
    {
        var failure = new InvalidOperationException("establishment lookup failed");
        SetUpAgentFailure("establishment", failure);

        var sut = CreateSut();
        var establishment = await ProvisionAsync("establishment");
        var context = new AgentContext();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunParallelAsync(
            [StepFor(establishment, _ => Task.FromResult("Establishment evidence."))],
            context, shouldSuppress: _ => false, cancellationToken: cancellationToken));

        Assert.Same(failure, thrown);
    }

    [Fact]
    public async Task RunParallelAsync_RecordsFailure_WhenAgentPausesOnUnresolvedToolCall()
    { 
        SetUpAgentResponse("toolcaller", ResponseWithFunctionCall("resp-1", "call-1", "lookup"));

        var sut = CreateSut();
        var toolcaller = await ProvisionAsync("toolcaller");
        var context = new AgentContext();

        var result = await sut.RunParallelAsync(
            [StepFor(toolcaller, _ => Task.FromResult("prompt"))],
            context, cancellationToken: cancellationToken);

        var step = Assert.Single(result.Results);
        Assert.False(step.Succeeded);
        Assert.IsType<InvalidOperationException>(step.Error);
        Assert.Contains("resolveToolCalls", step.Error!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunParallelAsync_NonMandatoryFailure_IsRecordedNotFatal_AndExcludedFromFinalOutput()
    {
        SetUpAgentResponse("ofsted", CompletedResponse("o1", "Ofsted findings."));
        SetUpAgentFailure("trust", new InvalidOperationException("search index unavailable"));
        SetUpAgentResponse("recast", CompletedResponse("r1", "Recast findings."));

        var sut = CreateSut();
        var context = new AgentContext();
        var agents = new[] { await ProvisionAsync("ofsted"), await ProvisionAsync("trust"), await ProvisionAsync("recast") };

        var result = await sut.RunParallelAsync(agents, "shared input", context,
            cancellationToken: cancellationToken);

        Assert.Equal(3, result.Results.Count);
        Assert.True(result.Results.Single(r => r.AgentName == "ofsted").Succeeded);
        Assert.False(result.Results.Single(r => r.AgentName == "trust").Succeeded);
        Assert.True(result.Results.Single(r => r.AgentName == "recast").Succeeded);
        Assert.Equal($"Ofsted findings.{Environment.NewLine}{Environment.NewLine}Recast findings.", result.FinalOutput);
    }

    [Fact]
    public async Task RunParallelAsync_MandatoryPlusSpecialists_MirrorsBriefingOrchestratorShape()
    {
        SetUpAgentResponse("establishment", CompletedResponse("e1", "Establishment info."));
        SetUpAgentResponse("ofsted", CompletedResponse("o1", "Ofsted findings."));
        SetUpAgentFailure("trust", new InvalidOperationException("search index unavailable"));

        var sut = CreateSut();
        var establishment = await ProvisionAsync("establishment");
        var ofsted = await ProvisionAsync("ofsted");
        var trust = await ProvisionAsync("trust");
        var context = new AgentContext();

        var mandatoryOutcome = await sut.RunParallelAsync(
            [StepFor(establishment, _ => Task.FromResult("Establishment evidence."))],
            context, shouldSuppress: _ => false, cancellationToken: cancellationToken);
        var specialistOutcome = await sut.RunParallelAsync(
            [
                StepFor(ofsted, _ => Task.FromResult("Ofsted evidence.")),
                StepFor(trust, _ => Task.FromResult("Trust evidence.")),
            ],
            context, cancellationToken: cancellationToken);

        var allResults = mandatoryOutcome.Results.Concat(specialistOutcome.Results).ToList();

        Assert.Equal(3, allResults.Count);
        Assert.True(allResults.Single(r => r.AgentName == "establishment").Succeeded);
        Assert.True(allResults.Single(r => r.AgentName == "ofsted").Succeeded);
        Assert.False(allResults.Single(r => r.AgentName == "trust").Succeeded);
    }
}
