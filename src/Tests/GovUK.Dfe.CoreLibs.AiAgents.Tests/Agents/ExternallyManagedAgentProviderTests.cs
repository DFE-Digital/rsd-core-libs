using GovUK.Dfe.CoreLibs.AiAgents.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Agents;

public sealed class ExternallyManagedAgentProviderTests
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;
    private readonly IAgentFactory _agentFactory = Substitute.For<IAgentFactory>();
    private readonly IAgentRuntime _agentRuntime = Substitute.For<IAgentRuntime>();

    private ExternallyManagedAgentProvider CreateSut(string agentName = "ofsted-agent")
        => new(agentName, _agentFactory, _agentRuntime);

    [Fact]
    public void AgentName_ReturnsTheNameItWasConstructedWith()
    {
        var sut = CreateSut("rise-concerns-agent");

        Assert.Equal("rise-concerns-agent", sut.AgentName);
    }

    [Fact]
    public async Task GetAgentAsync_ResolvesByName_ThroughTheRuntime_ApplyingAnyConfiguredPin()
    {
        var resolved = new AgentReference("resolved-id", "ofsted-agent", "3");
        _agentRuntime.ResolveAsync("ofsted-agent", Arg.Any<CancellationToken>()).Returns(resolved);

        var sut = CreateSut();
        var result = await sut.GetAgentAsync(cancellationToken);

        Assert.Same(resolved, result);
        await _agentRuntime.Received(1).ResolveAsync("ofsted-agent", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAgentAsync_NeverCreatesOrModifiesAnything()
    {
        _agentRuntime.ResolveAsync("ofsted-agent", Arg.Any<CancellationToken>())
            .Returns(new AgentReference("resolved-id", "ofsted-agent", "3"));

        var sut = CreateSut();
        await sut.GetAgentAsync(cancellationToken);

        await _agentFactory.DidNotReceiveWithAnyArgs().GetOrCreateAsync(default!, cancellationToken);
    }

    [Fact]
    public async Task GetLatestAgentAsync_ResolvesTheLatestVersion_ThroughTheFactory_IgnoringAnyPin()
    {
        var latest = new AgentReference("latest-id", "ofsted-agent", "5");
        _agentFactory.ResolveLatestAsync("ofsted-agent", Arg.Any<CancellationToken>()).Returns(latest);

        var sut = CreateSut();
        var result = await sut.GetLatestAgentAsync(cancellationToken);

        Assert.Same(latest, result);
        await _agentFactory.Received(1).ResolveLatestAsync("ofsted-agent", Arg.Any<CancellationToken>());
        await _agentRuntime.DidNotReceiveWithAnyArgs().ResolveAsync(default(string)!, cancellationToken);
    }

    [Fact]
    public async Task GetLatestAgentAsync_NeverCreatesOrModifiesAnything()
    {
        _agentFactory.ResolveLatestAsync("ofsted-agent", Arg.Any<CancellationToken>())
            .Returns(new AgentReference("latest-id", "ofsted-agent", "5"));

        var sut = CreateSut();
        await sut.GetLatestAgentAsync(cancellationToken);

        await _agentFactory.DidNotReceiveWithAnyArgs().GetOrCreateAsync(default!, cancellationToken);
    }
}
