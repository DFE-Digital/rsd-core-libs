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
    private readonly CancellationToken cancellationToken = default;
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

    [Fact]
    public async Task GetOrCreateAsync_WhenPinned_ResolvesThePin_WithoutBuildingASpecOrCreating()
    {
        var versionPinning = new AgentVersionPinningOptions { VersionPins = new Dictionary<string, string> { ["my-agent"] = "5" } };
        _agentFactory.ResolveAsync("my-agent", "5", Arg.Any<CancellationToken>()).Returns(new AgentReference("id-5", "my-agent", "5"));
        var specBuilt = false;

        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner), versionPinning);
        var result = await sut.GetOrCreateAsync("my-agent", _ =>
        {
            specBuilt = true;
            return Task.FromResult(new AgentSpec { Name = "my-agent", Instructions = "x" });
        }, cancellationToken);

        Assert.Equal("5", result.Version);
        Assert.False(specBuilt);
        await _agentFactory.DidNotReceiveWithAnyArgs().GetOrCreateAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenUnpinned_GetsOrCreatesTheBuiltSpec()
    {
        var spec = new AgentSpec { Name = "my-agent", Instructions = "Do the thing." };
        _agentFactory.GetOrCreateAsync(spec, Arg.Any<CancellationToken>()).Returns(new AgentReference("id-1", "my-agent", "1"));

        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner));
        var result = await sut.GetOrCreateAsync("my-agent", _ => Task.FromResult(spec), cancellationToken);

        Assert.Equal("1", result.Version);
    }

    [Fact]
    public async Task RunEphemeralAsync_StillDeletesTheAgent_WhenTheCallerCancels()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _agentRunner.RunAsync(Arg.Any<AgentSpec>(), "prompt", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AgentResult>(new OperationCanceledException(cancelled.Token)));

        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.RunEphemeralAsync(new AgentSpec { Name = "my-agent", Instructions = "x" }, "prompt", cancelled.Token));

        await _agentFactory.Received(1).DeleteAgentAsync(Arg.Is<string>(name => name.StartsWith("my-agent-")),
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested));
    }

    [Theory]
    [InlineData("web-search-agent-0f8fad5bd9cb469fa16570867728950e", true)]
    [InlineData("web-search-agent", false)]
    [InlineData("ofsted-agent-v2", false)]
    [InlineData("web-search-agent-0f8fad5b-d9cb-469f-a165-70867728950e", false)]
    public void IsEphemeralName_MatchesOnlyTheNamesRunEphemeralAsyncCreates(string name, bool expected)
        => Assert.Equal(expected, AgentRuntime.IsEphemeralName(name));

    [Fact]
    public async Task DeleteOrphanedEphemeralAgentsAsync_DeletesOnlyThisApplicationsEphemeralAgents()
    {
        var ownHash = AgentRuntime.ApplicationHashOf("briefing-tool");
        var otherHash = AgentRuntime.ApplicationHashOf("trust-insights-app");
        Func<string, bool>? predicate = null;
        _agentFactory.DeleteStaleAgentsAsync(Arg.Do<Func<string, bool>>(p => predicate = p), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner),
            runOptions: new AgentRunOptions { ApplicationName = "briefing-tool", RunTimeout = TimeSpan.FromMinutes(5) });
        await sut.DeleteOrphanedEphemeralAgentsAsync(TimeSpan.FromHours(1), cancellationToken);

        Assert.NotNull(predicate);
        Assert.True(predicate($"web-search-agent-{ownHash}0f8fad5bd9cb469fa1657086"));
        Assert.False(predicate($"web-search-agent-{otherHash}0f8fad5bd9cb469fa1657086"));
        Assert.False(predicate("ofsted-agent"));
    }

    [Fact]
    public async Task DeleteOrphanedEphemeralAgentsAsync_RefusesAMinimumAgeShorterThanARunCanTake()
    {
        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner),
            runOptions: new AgentRunOptions { RunTimeout = TimeSpan.FromMinutes(30) });

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.DeleteOrphanedEphemeralAgentsAsync(TimeSpan.FromMinutes(10), cancellationToken));
        await _agentFactory.DidNotReceiveWithAnyArgs().DeleteStaleAgentsAsync(default!, default, cancellationToken);
    }

    [Fact]
    public async Task RunEphemeralAsync_NamesTheAgentWithThisApplicationsHash_ThenARandomPart()
    {
        _agentRunner.RunAsync(Arg.Any<AgentSpec>(), "prompt", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new AgentResult(callInfo.Arg<AgentSpec>().Name, "output", 10)));
        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner),
            runOptions: new AgentRunOptions { ApplicationName = "briefing-tool" });

        await sut.RunEphemeralAsync(new AgentSpec { Name = "web-search-agent", Instructions = "x" }, "prompt", cancellationToken);
        await sut.RunEphemeralAsync(new AgentSpec { Name = "web-search-agent", Instructions = "x" }, "prompt", cancellationToken);

        var names = _agentRunner.ReceivedCalls().Select(call => ((AgentSpec)call.GetArguments()[0]!).Name).ToList();
        Assert.Equal(2, names.Distinct().Count());
        Assert.All(names, name =>
        {
            Assert.StartsWith($"web-search-agent-{AgentRuntime.ApplicationHashOf("briefing-tool")}", name, StringComparison.Ordinal);
            Assert.True(AgentRuntime.IsEphemeralName(name));
        });
    }
}
