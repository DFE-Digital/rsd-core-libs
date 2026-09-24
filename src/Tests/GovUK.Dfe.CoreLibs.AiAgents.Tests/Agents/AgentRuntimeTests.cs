using GovUK.Dfe.CoreLibs.AiAgents.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration;
using NSubstitute;
using Xunit;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Agents;

public sealed class AgentRuntimeTests
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;
    private readonly IAgentFactory _agentFactory = Substitute.For<IAgentFactory>();
    private readonly IAgentRunner _agentRunner = Substitute.For<IAgentRunner>();

    [Fact]
    public async Task ResolveAsync_ForwardsThePinnedVersion_WhenTheAgentIsPinned()
    {
        var versionPinning = new AgentVersionPinningOptions
        {
            VersionPins = new Dictionary<string, string> { ["my-agent"] = "5" },
        };
        _agentFactory.ResolveAsync("my-agent", "5", Arg.Any<CancellationToken>())
            .Returns(new AgentReference("agent-id", "my-agent", "5"));

        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner), versionPinning);

        var result = await sut.ResolveAsync("my-agent", cancellationToken);

        Assert.Equal(new AgentReference("agent-id", "my-agent", "5"), result);
    }

    [Fact]
    public async Task ResolveAsync_ForwardsNull_WhenTheAgentIsNotPinned()
    {
        _agentFactory.ResolveAsync("my-agent", null, Arg.Any<CancellationToken>())
            .Returns(new AgentReference("agent-id", "my-agent", "1"));

        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner), new AgentVersionPinningOptions());

        var result = await sut.ResolveAsync("my-agent", cancellationToken);

        Assert.Equal(new AgentReference("agent-id", "my-agent", "1"), result);
    }

    [Fact]
    public async Task ResolveAsync_DefaultsToUnpinned_WhenNoVersionPinningOptionsGiven()
    {
        _agentFactory.ResolveAsync("my-agent", null, Arg.Any<CancellationToken>())
            .Returns(new AgentReference("agent-id", "my-agent", "1"));

        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner));

        var result = await sut.ResolveAsync("my-agent", cancellationToken);

        Assert.Equal(new AgentReference("agent-id", "my-agent", "1"), result);
    }

    [Fact]
    public async Task ResolveAsync_WithACreatedReference_ReturnsItUnchanged_WhenTheAgentIsNotPinned()
    {
        var created = new AgentReference("created-id", "my-agent", "1");
        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner), new AgentVersionPinningOptions());

        var result = await sut.ResolveAsync(created, cancellationToken);
         
        Assert.Same(created, result);
        await _agentFactory.DidNotReceiveWithAnyArgs().ResolveAsync(default!, default, cancellationToken);
    }

    [Fact]
    public async Task ResolveAsync_WithACreatedReference_ResolvesThePinnedVersionInstead_WhenTheAgentIsPinned()
    {
        var created = new AgentReference("created-id", "my-agent", "1");
        var versionPinning = new AgentVersionPinningOptions
        {
            VersionPins = new Dictionary<string, string> { ["my-agent"] = "5" },
        };
        _agentFactory.ResolveAsync("my-agent", "5", Arg.Any<CancellationToken>())
            .Returns(new AgentReference("pinned-id", "my-agent", "5"));

        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner), versionPinning);

        var result = await sut.ResolveAsync(created, cancellationToken);

        Assert.Equal(new AgentReference("pinned-id", "my-agent", "5"), result);
    }

    [Fact]
    public void Orchestrator_ExposesTheGivenInstance()
    {
        var orchestrator = new AgentOrchestrator(_agentRunner);

        var sut = new AgentRuntime(_agentFactory, _agentRunner, orchestrator);

        Assert.Same(orchestrator, sut.Orchestrator);
    }

    [Fact]
    public async Task RunEphemeralAsync_CreatesWithAUniqueSuffixedName_AndReportsTheOriginalName()
    {
        AgentSpec? capturedSpec = null;
        _agentRunner.RunAsync(Arg.Do<AgentSpec>(spec => capturedSpec = spec), "prompt", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new AgentResult(callInfo.Arg<AgentSpec>().Name, "output", 10)));

        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner));
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };

        var result = await sut.RunEphemeralAsync(spec, "prompt", cancellationToken);

        Assert.NotNull(capturedSpec);
        Assert.NotEqual("my-agent", capturedSpec.Name);
        Assert.StartsWith("my-agent-", capturedSpec.Name, StringComparison.Ordinal);

        // The result reports the original name, not the unique Foundry-side one it ran under.
        Assert.Equal("my-agent", result.AgentName);
        Assert.Equal("output", result.Output);
    }

    [Fact]
    public async Task RunEphemeralAsync_DeletesTheAgent_UsingTheSameUniqueName_AfterASuccessfulRun()
    {
        AgentSpec? capturedSpec = null;
        _agentRunner.RunAsync(Arg.Do<AgentSpec>(spec => capturedSpec = spec), "prompt", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new AgentResult(callInfo.Arg<AgentSpec>().Name, "output", 10)));

        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner));
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };

        await sut.RunEphemeralAsync(spec, "prompt", cancellationToken);

        Assert.NotNull(capturedSpec);
        await _agentFactory.Received(1).DeleteAgentAsync(capturedSpec.Name, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunEphemeralAsync_StillDeletesTheAgent_AndPropagates_WhenTheRunFails()
    {
        AgentSpec? capturedSpec = null;
        _agentRunner.RunAsync(Arg.Do<AgentSpec>(spec => capturedSpec = spec), "prompt", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AgentResult>(new InvalidOperationException("boom")));

        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner));
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunEphemeralAsync(spec, "prompt", cancellationToken));

        Assert.NotNull(capturedSpec);
        await _agentFactory.Received(1).DeleteAgentAsync(capturedSpec.Name, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunEphemeralAsync_ReturnsTheRunResult_WhenDeleteFails_RatherThanMaskingItWithTheDeleteFailure()
    {
        _agentRunner.RunAsync(Arg.Any<AgentSpec>(), "prompt", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new AgentResult(callInfo.Arg<AgentSpec>().Name, "output", 10)));
        _agentFactory.DeleteAgentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("delete failed")));

        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner));
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };

        var result = await sut.RunEphemeralAsync(spec, "prompt", cancellationToken);

        Assert.Equal("my-agent", result.AgentName);
        Assert.Equal("output", result.Output);
    }
}
