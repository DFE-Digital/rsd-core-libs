using GovUK.Dfe.CoreLibs.AiAgents.Agents;
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
    public async Task DeleteOrphanedEphemeralAgentsAsync_RefusesAMinimumAgeShorterThanARunCanTake()
    {
        var sut = new AgentRuntime(_agentFactory, _agentRunner, new AgentOrchestrator(_agentRunner),
            runOptions: new AgentRunOptions { RunTimeout = TimeSpan.FromMinutes(30) });

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.DeleteOrphanedEphemeralAgentsAsync(TimeSpan.FromMinutes(10), cancellationToken));
        await _agentFactory.DidNotReceiveWithAnyArgs().DeleteStaleAgentsAsync(default!, default, cancellationToken);
    }
}
