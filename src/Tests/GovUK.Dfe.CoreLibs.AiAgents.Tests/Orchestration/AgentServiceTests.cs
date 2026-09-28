using GovUK.Dfe.CoreLibs.AiAgents.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration;
using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Tools;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenAI.Responses;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Orchestration;

public sealed class AgentServiceTests
{
    private static readonly AgentDefinition Managed = new("managed-agent", "ManagedPromptType");
    private static readonly AgentDefinition ManagedTwo = new("managed-agent-two", "ManagedPromptType");
    private static readonly AgentDefinition Ephemeral = new("ephemeral-agent", "EphemeralPromptType", IsManagedAgent: false);

    private readonly CancellationToken cancellationToken = default;
    private readonly IAgentFactory _agentFactory = Substitute.For<IAgentFactory>();
    private readonly IAgentRunner _agentRunner = Substitute.For<IAgentRunner>();
    private readonly IAgentRuntime _agentRuntime = Substitute.For<IAgentRuntime>();
    private readonly IPromptProvider _promptProvider = Substitute.For<IPromptProvider>();
    private readonly IManagedAgentProvider _managedAgentProvider = Substitute.For<IManagedAgentProvider>();

    /// <summary>Matches any tool-call callback argument (in place, like any other Arg.Any).</summary>
    private static ToolCallResolver? AnyResolver()
        => Arg.Any<ToolCallResolver?>();

    /// <summary>A provider for an agent created elsewhere: the service only resolves it.</summary>
    private AgentService CreateSut(params IManagedAgentProvider[] extraProviders)
    {
        _managedAgentProvider.AgentName.Returns(Managed.Name);
        _managedAgentProvider.CreatesAgent.Returns(false);
        _managedAgentProvider.GetAgentAsync(Arg.Any<CancellationToken>()).Returns(new AgentReference($"{Managed.Name}-id", Managed.Name));

        return new AgentService(_agentRunner, _agentRuntime, new AgentSpecBuilder(_promptProvider, customProviders: [_managedAgentProvider, .. extraProviders]));
    }

    private static Task<string> ResolvePrompt(AgentDefinition definition, CancellationToken _)
        => Task.FromResult($"prompt-for-{definition.Name}");

    // ===================== Single agent =====================

    [Fact]
    public async Task RunAsync_RunsOneAgent_WithItsEvidenceSentAsAdditionalContext()
    {
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == Managed.Name), "Summarise.", conversationId: Arg.Any<string?>(), additionalContext: Arg.Is<string?>("Rated Good."),
            resolveToolCalls: AnyResolver(), cancellationToken: Arg.Any<CancellationToken>()).Returns(new AgentResult(Managed.Name, "Good.", 10));

        var result = await CreateSut().RunAsync(Managed, "Summarise.", evidence: "Rated Good.", cancellationToken);

        Assert.Equal("Good.", result.Output);
    }

    [Fact]
    public async Task RunAsync_Throws_RatherThanReturningAFallback()
    {
        _agentRunner.RunAsync(Arg.Any<AgentReference>(), Arg.Any<string>(), conversationId: Arg.Any<string?>(), additionalContext: Arg.Any<string?>(),
            resolveToolCalls: AnyResolver(), cancellationToken: Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut().RunAsync(Managed, "Summarise.", cancellationToken: cancellationToken));
    }

    // ===================== Parallel =====================

    [Fact]
    public async Task RunParallelAsync_RunsAnExternallyManagedAgent_ThroughItsProvider_WithoutCreatingAnything()
    {
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == Managed.Name), "prompt-for-managed-agent",
            cancellationToken: Arg.Any<CancellationToken>()).Returns(new AgentResult(Managed.Name, "managed output", 10));

        var results = await CreateSut().RunParallelAsync([Managed], ResolvePrompt, new AgentContext(), cancellationToken: cancellationToken);

        Assert.Equal("managed output", Assert.Single(results).Output);
        await _managedAgentProvider.Received(1).GetAgentAsync(Arg.Any<CancellationToken>());
        await _agentFactory.DidNotReceiveWithAnyArgs().GetOrCreateAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task RunParallelAsync_GetsOrCreatesAManagedAgent_FromItsDefinition_ByDefault()
    {
        var created = new AgentReference($"{Managed.Name}-created-id", Managed.Name, "1");
        _promptProvider.GetSystemPrompt(Managed.SystemPromptType).Returns("managed instructions");
        _agentFactory.GetOrCreateAsync(Arg.Is<AgentSpec>(spec => spec.Name == Managed.Name && spec.Instructions == "managed instructions"),
            Arg.Any<CancellationToken>()).Returns(created);
        _agentRunner.RunAsync(created, "prompt-for-managed-agent", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new AgentResult(Managed.Name, "managed output", 10));

        var runtime = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner));
        var sut = new AgentService(_agentRunner, runtime, new AgentSpecBuilder(_promptProvider));
        var results = await sut.RunParallelAsync([Managed], ResolvePrompt, new AgentContext(), cancellationToken: cancellationToken);

        Assert.Equal("managed output", Assert.Single(results).Output);
    }

    [Fact]
    public async Task RunParallelAsync_SendsEachAgentsEvidence_AsAdditionalContext()
    {
        _agentRunner.RunAsync(Arg.Any<AgentReference>(), Arg.Any<string>(), conversationId: Arg.Any<string?>(), additionalContext: Arg.Is<string?>("evidence-for-managed-agent"),
            resolveToolCalls: AnyResolver(), cancellationToken: Arg.Any<CancellationToken>()).Returns(new AgentResult(Managed.Name, "ok", 1));

        var results = await CreateSut().RunParallelAsync([Managed], ResolvePrompt, new AgentContext(), cancellationToken: cancellationToken,
            resolveEvidence: (definition, _) => Task.FromResult<string?>($"evidence-for-{definition.Name}"));

        Assert.Equal("ok", Assert.Single(results).Output);
    }

    // ===================== Sequential =====================

    [Fact]
    public async Task RunSequentialAsync_SendsEachEarlierOutput_AsEvidence_NeverInThePrompt()
    {
        var managedTwoProvider = Substitute.For<IManagedAgentProvider>();
        managedTwoProvider.AgentName.Returns(ManagedTwo.Name);
        managedTwoProvider.CreatesAgent.Returns(false);
        managedTwoProvider.GetAgentAsync(Arg.Any<CancellationToken>()).Returns(new AgentReference($"{ManagedTwo.Name}-id", ManagedTwo.Name));
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == Managed.Name), "prompt-for-managed-agent", conversationId: Arg.Any<string?>(), additionalContext: Arg.Is<string?>("initial"),
            resolveToolCalls: AnyResolver(), cancellationToken: Arg.Any<CancellationToken>()).Returns(new AgentResult(Managed.Name, "first output", 10));
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == ManagedTwo.Name), "prompt-for-managed-agent-two", conversationId: Arg.Any<string?>(), additionalContext: Arg.Is<string?>("first output"),
            resolveToolCalls: AnyResolver(), cancellationToken: Arg.Any<CancellationToken>()).Returns(new AgentResult(ManagedTwo.Name, "second output", 10));

        var context = new AgentContext();
        var results = await CreateSut(managedTwoProvider).RunSequentialAsync([Managed, ManagedTwo], ResolvePrompt, "initial",
            context, cancellationToken: cancellationToken);

        Assert.Equal(["first output", "second output"], results.Select(r => r.Output));
        Assert.Equal([Managed.Name, ManagedTwo.Name], context.History.Select(entry => entry.AgentName));
    }

    [Fact]
    public async Task RunSequentialAsync_AfterAFailure_TheNextAgentGetsTheLastSuccessfulOutput()
    {
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == Managed.Name), Arg.Any<string>(), conversationId: Arg.Any<string?>(), additionalContext: Arg.Any<string?>(),
            resolveToolCalls: AnyResolver(), cancellationToken: Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("boom"));
        _agentRuntime.RunEphemeralAsync(Arg.Is<AgentSpec>(spec => spec.Name == Ephemeral.Name), "prompt-for-ephemeral-agent", AnyResolver(),
            "initial", Arg.Any<Func<AgentResult, string?>?>(), Arg.Any<CancellationToken>()).Returns(new AgentResult(Ephemeral.Name, "ephemeral output", 5));

        var results = await CreateSut().RunSequentialAsync([Managed, Ephemeral], ResolvePrompt, "initial",
            new AgentContext(), cancellationToken: cancellationToken);

        Assert.Contains("could not be generated", results[0].Output, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ephemeral output", results[1].Output);
    }

    [Fact]
    public async Task RunSequentialAsync_PropagatesAFailure_WhenShouldSuppressReturnsFalse()
    {
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == Managed.Name), Arg.Any<string>(), conversationId: Arg.Any<string?>(), additionalContext: Arg.Any<string?>(),
            resolveToolCalls: AnyResolver(), cancellationToken: Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().RunSequentialAsync([Managed], ResolvePrompt, "initial", new AgentContext(),
                shouldSuppress: _ => false, cancellationToken: cancellationToken));
    }

    // ===================== Specs and tools =====================

    [Fact]
    public async Task RunParallelAsync_AttachesTheBoundToolProvidersTools_ToAnEphemeralAgentsSpec()
    {
        var tool = ResponseTool.CreateWebSearchTool();
        var toolProvider = Substitute.For<IAgentToolProvider>();
        toolProvider.GetToolsAsync(Arg.Any<CancellationToken>()).Returns([tool]);
        AgentSpec? capturedSpec = null;
        _agentRuntime.RunEphemeralAsync(Arg.Do<AgentSpec>(spec => capturedSpec = spec), Arg.Any<string>(), AnyResolver(),
            Arg.Any<string?>(), Arg.Any<Func<AgentResult, string?>?>(), Arg.Any<CancellationToken>()).Returns(new AgentResult(Ephemeral.Name, "ephemeral output", 5));

        var sut = new AgentService(_agentRunner, _agentRuntime, new AgentSpecBuilder(_promptProvider, [new AgentToolBinding(Ephemeral.Name, toolProvider)]));
        await sut.RunParallelAsync([Ephemeral], ResolvePrompt, new AgentContext(), cancellationToken: cancellationToken);

        Assert.Same(tool, Assert.Single(capturedSpec!.Tools));
    }

    [Fact]
    public async Task RunParallelAsync_AppliesTheDefinitionsAllowedToolsAndSchema_ToACustomProvidersSpec()
    {
        var allowed = ResponseTool.CreateFunctionTool("get_performance_data", BinaryData.FromString("""{"type":"object"}"""), false);
        var notAllowed = ResponseTool.CreateFunctionTool("update_school_record", BinaryData.FromString("""{"type":"object"}"""), false);
        var custom = Substitute.For<IManagedAgentProvider>();
        custom.AgentName.Returns("custom-agent");
        custom.CreatesAgent.Returns(true);
        custom.BuildSpecAsync(Arg.Any<CancellationToken>()).Returns(new AgentSpec
        {
            Name = "custom-agent", Instructions = "Custom.", Model = "my-connection/gpt-4.1", Tools = [allowed, notAllowed],
        });
        var schema = AgentOutputSchema.For<Findings>("findings");
        var definition = new AgentDefinition("custom-agent", "Unused") { AllowedTools = ["get_performance_data"], OutputSchema = schema };

        AgentSpec? built = null;
        _agentRuntime.GetOrCreateAsync("custom-agent", Arg.Any<Func<CancellationToken, Task<AgentSpec>>>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                built = await callInfo.Arg<Func<CancellationToken, Task<AgentSpec>>>()(cancellationToken);
                return new AgentReference("id", "custom-agent", "1");
            });
        _agentRunner.RunAsync(Arg.Any<AgentReference>(), Arg.Any<string>(), conversationId: Arg.Any<string?>(), additionalContext: Arg.Any<string?>(),
            resolveToolCalls: AnyResolver(), cancellationToken: Arg.Any<CancellationToken>()).Returns(new AgentResult("custom-agent", "{}", 1));

        var sut = new AgentService(_agentRunner, _agentRuntime, new AgentSpecBuilder(_promptProvider, customProviders: [custom]));
        await sut.RunParallelAsync([definition], ResolvePrompt, new AgentContext(), cancellationToken: cancellationToken);

        Assert.Equal("my-connection/gpt-4.1", built!.Model);
        Assert.Equal(["get_performance_data"], built.Tools.OfType<FunctionTool>().Select(tool => tool.FunctionName));
        Assert.Same(schema, built.OutputSchema);
    }

    private sealed record Findings(string Rating, string? InspectionDate);
}
