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

public sealed class SpecialistAgentRunnerTests
{
    private static readonly AgentDefinition Managed = new("managed-agent", "ManagedPromptType");
    private static readonly AgentDefinition Ephemeral = new("ephemeral-agent", "EphemeralPromptType", IsManagedAgent: false);

    private readonly CancellationToken cancellationToken = default;
    private readonly IAgentFactory _agentFactory = Substitute.For<IAgentFactory>();
    private readonly IAgentRunner _agentRunner = Substitute.For<IAgentRunner>();
    private readonly IAgentRuntime _agentRuntime = Substitute.For<IAgentRuntime>();
    private readonly IPromptProvider _promptProvider = Substitute.For<IPromptProvider>();
    private readonly IManagedAgentProvider _managedAgentProvider = Substitute.For<IManagedAgentProvider>();

    private SpecialistAgentRunner CreateSut(bool withCustomProvider = true)
    {
        _agentRuntime.Orchestrator.Returns(new AgentOrchestrator(_agentRunner));

        _managedAgentProvider.AgentName.Returns(Managed.Name);
        _managedAgentProvider.GetAgentAsync(Arg.Any<CancellationToken>())
            .Returns(new AgentReference($"{Managed.Name}-id", Managed.Name));

        return new SpecialistAgentRunner(_agentFactory, _agentRunner, _agentRuntime, _promptProvider,
            withCustomProvider ? [_managedAgentProvider] : []);
    }

    private static Task<string> ResolvePrompt(AgentDefinition definition, CancellationToken _)
        => Task.FromResult($"prompt-for-{definition.Name}");

    [Fact]
    public async Task RunAsync_RunsAManagedAgent_ByResolvingItThroughItsCustomProvider_WhenOneIsRegistered()
    {
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == Managed.Name), "prompt-for-managed-agent",
            cancellationToken: Arg.Any<CancellationToken>()).Returns(new AgentResult(Managed.Name, "managed output", 10));

        var sut = CreateSut();
        var results = await sut.RunParallelAsync([Managed], ResolvePrompt, new AgentContext(), cancellationToken: cancellationToken);

        var result = Assert.Single(results);
        Assert.Equal(Managed.Name, result.AgentName);
        Assert.Equal("managed output", result.Output);
        await _managedAgentProvider.Received(1).GetAgentAsync(Arg.Any<CancellationToken>());
        await _agentFactory.DidNotReceiveWithAnyArgs().GetOrCreateAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task RunAsync_RunsAManagedAgent_ByDefault_WhenNoCustomProviderIsRegisteredForItsName()
    {
        var created = new AgentReference($"{Managed.Name}-created-id", Managed.Name, "1");
        _promptProvider.GetSystemPrompt(Managed.SystemPromptType).Returns("managed instructions");
        _agentFactory.GetOrCreateAsync(
            Arg.Is<AgentSpec>(spec => spec.Name == Managed.Name && spec.Instructions == "managed instructions"),
            Arg.Any<CancellationToken>()).Returns(created);
        _agentRuntime.ResolveAsync(created, Arg.Any<CancellationToken>())
            .Returns(new AgentReference($"{Managed.Name}-resolved-id", Managed.Name, "1"));
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Id == $"{Managed.Name}-resolved-id"), "prompt-for-managed-agent",
            cancellationToken: Arg.Any<CancellationToken>()).Returns(new AgentResult(Managed.Name, "managed output", 10));

        var sut = CreateSut(withCustomProvider: false);
        var results = await sut.RunParallelAsync([Managed], ResolvePrompt, new AgentContext(), cancellationToken: cancellationToken);

        var result = Assert.Single(results);
        Assert.Equal(Managed.Name, result.AgentName);
        Assert.Equal("managed output", result.Output);
        await _agentFactory.Received(1).GetOrCreateAsync(
            Arg.Is<AgentSpec>(spec => spec.Name == Managed.Name && spec.Instructions == "managed instructions"),
            Arg.Any<CancellationToken>());
        await _agentRuntime.Received(1).ResolveAsync(created, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_RunsAnEphemeralAgent_UsingRunEphemeralAsync_WithInstructionsFromThePromptProvider()
    {
        _promptProvider.GetSystemPrompt(Ephemeral.SystemPromptType).Returns("ephemeral instructions");
        _agentRuntime.RunEphemeralAsync(Arg.Is<AgentSpec>(spec => spec.Name == Ephemeral.Name && spec.Instructions == "ephemeral instructions"),
            "prompt-for-ephemeral-agent", Arg.Any<CancellationToken>())
            .Returns(new AgentResult(Ephemeral.Name, "ephemeral output", 5));

        var sut = new SpecialistAgentRunner(_agentFactory, _agentRunner, _agentRuntime, _promptProvider);
        var results = await sut.RunParallelAsync([Ephemeral], ResolvePrompt, new AgentContext(), cancellationToken: cancellationToken);

        var result = Assert.Single(results);
        Assert.Equal(Ephemeral.Name, result.AgentName);
        Assert.Equal("ephemeral output", result.Output);
    }

    [Fact]
    public async Task RunAsync_RunsManagedAndEphemeralDefinitions_ReturningBothResults()
    {
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == Managed.Name), Arg.Any<string>(),
            cancellationToken: Arg.Any<CancellationToken>()).Returns(new AgentResult(Managed.Name, "managed output", 10));
        _agentRuntime.RunEphemeralAsync(Arg.Is<AgentSpec>(spec => spec.Name == Ephemeral.Name), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AgentResult(Ephemeral.Name, "ephemeral output", 5));

        var sut = CreateSut();
        var results = await sut.RunParallelAsync([Managed, Ephemeral], ResolvePrompt, new AgentContext(), cancellationToken: cancellationToken);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.AgentName == Managed.Name && r.Output == "managed output");
        Assert.Contains(results, r => r.AgentName == Ephemeral.Name && r.Output == "ephemeral output");
    }

    [Fact]
    public async Task RunAsync_SuppressesAManagedAgentFailure_ByDefault_ReturningAFallbackResult()
    {
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == Managed.Name), Arg.Any<string>(),
            cancellationToken: Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("boom"));

        var sut = CreateSut();
        var results = await sut.RunParallelAsync([Managed], ResolvePrompt, new AgentContext(), cancellationToken: cancellationToken);

        var result = Assert.Single(results);
        Assert.Equal(Managed.Name, result.AgentName);
        Assert.Contains("could not be generated", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_SuppressesAnEphemeralAgentFailure_ByDefault_ReturningAFallbackResult()
    {
        _agentRuntime.RunEphemeralAsync(Arg.Is<AgentSpec>(spec => spec.Name == Ephemeral.Name), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var sut = new SpecialistAgentRunner(_agentFactory, _agentRunner, _agentRuntime, _promptProvider);
        var results = await sut.RunParallelAsync([Ephemeral], ResolvePrompt, new AgentContext(), cancellationToken: cancellationToken);

        var result = Assert.Single(results);
        Assert.Equal(Ephemeral.Name, result.AgentName);
        Assert.Contains("could not be generated", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_PropagatesAManagedAgentFailure_WhenShouldSuppressReturnsFalse()
    {
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == Managed.Name), Arg.Any<string>(),
            cancellationToken: Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("boom"));

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RunParallelAsync([Managed], ResolvePrompt, new AgentContext(), shouldSuppress: _ => false, cancellationToken: cancellationToken));
    }

    private static readonly AgentDefinition ManagedTwo = new("managed-agent-two", "ManagedPromptType");

    private static Task<string> ResolveSequentialPrompt(AgentDefinition definition, string previousOutput, CancellationToken _)
        => Task.FromResult($"prompt-for-{definition.Name}-given-{previousOutput}");

    [Fact]
    public async Task RunSequentialAsync_RunsEachDefinitionInOrder_FeedingEachOutputIntoTheNextPrompt()
    {
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == Managed.Name), "prompt-for-managed-agent-given-initial",
            cancellationToken: Arg.Any<CancellationToken>()).Returns(new AgentResult(Managed.Name, "first output", 10));
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == ManagedTwo.Name), "prompt-for-managed-agent-two-given-first output",
            cancellationToken: Arg.Any<CancellationToken>()).Returns(new AgentResult(ManagedTwo.Name, "second output", 10));

        var managedTwoProvider = Substitute.For<IManagedAgentProvider>();
        managedTwoProvider.AgentName.Returns(ManagedTwo.Name);
        managedTwoProvider.GetAgentAsync(Arg.Any<CancellationToken>()).Returns(new AgentReference($"{ManagedTwo.Name}-id", ManagedTwo.Name));

        _agentRuntime.Orchestrator.Returns(new AgentOrchestrator(_agentRunner));
        _managedAgentProvider.AgentName.Returns(Managed.Name);
        _managedAgentProvider.GetAgentAsync(Arg.Any<CancellationToken>()).Returns(new AgentReference($"{Managed.Name}-id", Managed.Name));

        var sut = new SpecialistAgentRunner(_agentFactory, _agentRunner, _agentRuntime, _promptProvider,
            [_managedAgentProvider, managedTwoProvider]);

        var results = await sut.RunSequentialAsync([Managed, ManagedTwo], ResolveSequentialPrompt, "initial",
            new AgentContext(), cancellationToken: cancellationToken);

        Assert.Equal(2, results.Count);
        Assert.Equal(Managed.Name, results[0].AgentName);
        Assert.Equal("first output", results[0].Output);
        Assert.Equal(ManagedTwo.Name, results[1].AgentName);
        Assert.Equal("second output", results[1].Output);
    }

    [Fact]
    public async Task RunSequentialAsync_ThreadsOutputAcrossManagedAndEphemeralSteps_InTheSameChain()
    {
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == Managed.Name), "prompt-for-managed-agent-given-initial",
            cancellationToken: Arg.Any<CancellationToken>()).Returns(new AgentResult(Managed.Name, "managed output", 10));
        _agentRuntime.RunEphemeralAsync(Arg.Is<AgentSpec>(spec => spec.Name == Ephemeral.Name),
            "prompt-for-ephemeral-agent-given-managed output", Arg.Any<CancellationToken>())
            .Returns(new AgentResult(Ephemeral.Name, "ephemeral output", 5));

        var sut = CreateSut();

        var results = await sut.RunSequentialAsync([Managed, Ephemeral], ResolveSequentialPrompt, "initial",
            new AgentContext(), cancellationToken: cancellationToken);

        Assert.Equal(2, results.Count);
        Assert.Equal("managed output", results[0].Output);
        Assert.Equal("ephemeral output", results[1].Output);
    }

    [Fact]
    public async Task RunSequentialAsync_SuppressesAFailure_AndFeedsTheLastSuccessfulOutputForward_ByDefault()
    {
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == Managed.Name), Arg.Any<string>(),
            cancellationToken: Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("boom"));
        _agentRuntime.RunEphemeralAsync(Arg.Is<AgentSpec>(spec => spec.Name == Ephemeral.Name),
            "prompt-for-ephemeral-agent-given-initial", Arg.Any<CancellationToken>())
            .Returns(new AgentResult(Ephemeral.Name, "ephemeral output", 5));

        var sut = CreateSut();

        var results = await sut.RunSequentialAsync([Managed, Ephemeral], ResolveSequentialPrompt, "initial",
            new AgentContext(), cancellationToken: cancellationToken);

        Assert.Equal(2, results.Count);
        Assert.Contains("could not be generated", results[0].Output, StringComparison.OrdinalIgnoreCase); 
        Assert.Equal("ephemeral output", results[1].Output);
    }

    [Fact]
    public async Task RunSequentialAsync_PropagatesAFailure_WhenShouldSuppressReturnsFalse()
    {
        _agentRunner.RunAsync(Arg.Is<AgentReference>(a => a.Name == Managed.Name), Arg.Any<string>(),
            cancellationToken: Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("boom"));

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RunSequentialAsync([Managed], ResolveSequentialPrompt, "initial", new AgentContext(),
                shouldSuppress: _ => false, cancellationToken: cancellationToken));
    }

    [Fact]
    public async Task RunParallelAsync_AttachesNoTools_WhenNoBindingIsRegisteredForTheAgentsName()
    {
        AgentSpec? capturedSpec = null;
        _agentRuntime.RunEphemeralAsync(Arg.Do<AgentSpec>(spec => capturedSpec = spec), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AgentResult(Ephemeral.Name, "ephemeral output", 5));

        var sut = new SpecialistAgentRunner(_agentFactory, _agentRunner, _agentRuntime, _promptProvider);
        await sut.RunParallelAsync([Ephemeral], ResolvePrompt, new AgentContext(), cancellationToken: cancellationToken);

        Assert.NotNull(capturedSpec);
        Assert.Empty(capturedSpec.Tools);
    }

    [Fact]
    public async Task RunParallelAsync_AttachesTheBoundToolProvidersTools_ToAnEphemeralAgentsSpec()
    {
        var tool = ResponseTool.CreateWebSearchTool();
        var toolProvider = Substitute.For<IAgentToolProvider>();
        toolProvider.GetToolsAsync(Arg.Any<CancellationToken>()).Returns([tool]);

        AgentSpec? capturedSpec = null;
        _agentRuntime.RunEphemeralAsync(Arg.Do<AgentSpec>(spec => capturedSpec = spec), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AgentResult(Ephemeral.Name, "ephemeral output", 5));

        var sut = new SpecialistAgentRunner(_agentFactory, _agentRunner, _agentRuntime, _promptProvider,
            toolBindings: [new AgentToolBinding(Ephemeral.Name, toolProvider)]);

        await sut.RunParallelAsync([Ephemeral], ResolvePrompt, new AgentContext(), cancellationToken: cancellationToken);

        Assert.NotNull(capturedSpec);
        Assert.Same(tool, Assert.Single(capturedSpec.Tools));
    }

    [Fact]
    public async Task RunParallelAsync_AttachesTheBoundToolProvidersTools_ToAManagedAgentsSpec_WhenCreatedByDefault()
    {
        var tool = ResponseTool.CreateWebSearchTool();
        var toolProvider = Substitute.For<IAgentToolProvider>();
        toolProvider.GetToolsAsync(Arg.Any<CancellationToken>()).Returns([tool]);

        var created = new AgentReference($"{Managed.Name}-created-id", Managed.Name, "1");
        _agentFactory.GetOrCreateAsync(Arg.Any<AgentSpec>(), Arg.Any<CancellationToken>()).Returns(created);
        _agentRuntime.ResolveAsync(created, Arg.Any<CancellationToken>()).Returns(created);
        _agentRuntime.Orchestrator.Returns(new AgentOrchestrator(_agentRunner));
        _agentRunner.RunAsync(Arg.Any<AgentReference>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new AgentResult(Managed.Name, "managed output", 10));

        var sut = new SpecialistAgentRunner(_agentFactory, _agentRunner, _agentRuntime, _promptProvider,
            toolBindings: [new AgentToolBinding(Managed.Name, toolProvider)]);

        await sut.RunParallelAsync([Managed], ResolvePrompt, new AgentContext(), cancellationToken: cancellationToken);

        await _agentFactory.Received(1).GetOrCreateAsync(
            Arg.Is<AgentSpec>(spec => spec.Tools.Count == 1 && spec.Tools[0] == tool), Arg.Any<CancellationToken>());
    }
}
