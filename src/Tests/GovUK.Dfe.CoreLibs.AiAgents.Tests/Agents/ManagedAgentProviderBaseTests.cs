using GovUK.Dfe.CoreLibs.AiAgents.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Agents;

public sealed class ManagedAgentProviderBaseTests
{
    private sealed class TestManagedAgentProvider(IAgentFactory factory, IAgentRuntime runtime)
        : ManagedAgentProviderBase(factory, runtime)
    {
        public override string AgentName => "my-agent";

        protected override AgentSpec BuildSpec() => new() { Name = AgentName, Instructions = "Do the thing." };
    }

    private readonly CancellationToken cancellationToken = default;
    private readonly IAgentFactory _factory = Substitute.For<IAgentFactory>();

    [Fact]
    public async Task GetAgentAsync_WhenPinned_ResolvesThePinWithoutCreatingAnything()
    {
        var pinning = new AgentVersionPinningOptions { VersionPins = new Dictionary<string, string> { ["my-agent"] = "3" } };
        _factory.ResolveAsync("my-agent", "3", Arg.Any<CancellationToken>()).Returns(new AgentReference("v3-id", "my-agent", "3"));
        var runner = Substitute.For<IAgentRunner>();
        var runtime = new AgentRuntime(_factory, runner, new AgentOrchestrator(runner), pinning);

        var result = await new TestManagedAgentProvider(_factory, runtime).GetAgentAsync(cancellationToken);

        Assert.Equal("3", result.Version);
        await _factory.DidNotReceiveWithAnyArgs().GetOrCreateAsync(default!, cancellationToken);
    }
}
