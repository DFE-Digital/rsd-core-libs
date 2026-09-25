using GovUK.Dfe.CoreLibs.AiAgents.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
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
    private readonly IAgentRuntime _runtime = Substitute.For<IAgentRuntime>();

    private TestManagedAgentProvider CreateSut() => new(_factory, _runtime);

    [Fact]
    public async Task GetAgentAsync_EnsuresCreated_ThenAsksTheRuntimeToResolveThatExactCreatedReference()
    {
        var created = new AgentReference("created-id", "my-agent", "1");
        _factory.GetOrCreateAsync(Arg.Is<AgentSpec>(spec => spec.Name == "my-agent"), Arg.Any<CancellationToken>())
            .Returns(created);
        _runtime.ResolveAsync(created, Arg.Any<CancellationToken>())
            .Returns(new AgentReference("pinned-id", "my-agent", "2"));

        var sut = CreateSut();

        var result = await sut.GetAgentAsync(cancellationToken);

        Assert.Equal(new AgentReference("pinned-id", "my-agent", "2"), result);
        await _factory.Received(1).GetOrCreateAsync(Arg.Is<AgentSpec>(spec => spec.Name == "my-agent"), Arg.Any<CancellationToken>());
        await _runtime.Received(1).ResolveAsync(created, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLatestAgentAsync_ReturnsWhatGetOrCreateAsyncReturned_WithoutAnyFurtherResolution()
    {
        var created = new AgentReference("created-id", "my-agent", "1");
        _factory.GetOrCreateAsync(Arg.Is<AgentSpec>(spec => spec.Name == "my-agent"), Arg.Any<CancellationToken>())
            .Returns(created);

        var sut = CreateSut();

        var result = await sut.GetLatestAgentAsync(cancellationToken);
         
        Assert.Equal(created, result);
        await _factory.Received(1).GetOrCreateAsync(Arg.Is<AgentSpec>(spec => spec.Name == "my-agent"), Arg.Any<CancellationToken>());
        await _factory.DidNotReceiveWithAnyArgs().ResolveLatestAsync(default!, cancellationToken);
        await _runtime.DidNotReceiveWithAnyArgs().ResolveAsync(default(AgentReference)!, cancellationToken);
    }
}
