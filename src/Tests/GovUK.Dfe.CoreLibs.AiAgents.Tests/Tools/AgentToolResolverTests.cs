using GovUK.Dfe.CoreLibs.AiAgents.Tools;
using NSubstitute;
using OpenAI.Responses;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Tools;

public sealed class AgentToolResolverTests
{
    private readonly CancellationToken cancellationToken = default;

    [Fact]
    public async Task ResolveAsync_ReturnsEmpty_WhenNoBindingIsRegisteredForTheAgentName()
    {
        var toolProviders = AgentToolResolver.GroupByAgentName(null);

        var tools = await AgentToolResolver.ResolveAsync(toolProviders, "some-agent", cancellationToken);

        Assert.Empty(tools);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsOnlyTheToolsBoundToThatAgentsName()
    {
        var toolForA = ResponseTool.CreateWebSearchTool();
        var toolForB = ResponseTool.CreateWebSearchTool();
        var providerA = Substitute.For<IAgentToolProvider>();
        providerA.GetToolsAsync(Arg.Any<CancellationToken>()).Returns([toolForA]);
        var providerB = Substitute.For<IAgentToolProvider>();
        providerB.GetToolsAsync(Arg.Any<CancellationToken>()).Returns([toolForB]);

        var toolProviders = AgentToolResolver.GroupByAgentName([
            new AgentToolBinding("agent-a", providerA),
            new AgentToolBinding("agent-b", providerB),
        ]);

        var toolsForA = await AgentToolResolver.ResolveAsync(toolProviders, "agent-a", cancellationToken);

        Assert.Same(toolForA, Assert.Single(toolsForA));
    }

    [Fact]
    public async Task ResolveAsync_CombinesTools_WhenMultipleBindingsShareTheSameAgentName()
    {
        var toolOne = ResponseTool.CreateWebSearchTool();
        var toolTwo = ResponseTool.CreateWebSearchTool();
        var providerOne = Substitute.For<IAgentToolProvider>();
        providerOne.GetToolsAsync(Arg.Any<CancellationToken>()).Returns([toolOne]);
        var providerTwo = Substitute.For<IAgentToolProvider>();
        providerTwo.GetToolsAsync(Arg.Any<CancellationToken>()).Returns([toolTwo]);

        var toolProviders = AgentToolResolver.GroupByAgentName([
            new AgentToolBinding("web-search-agent", providerOne),
            new AgentToolBinding("web-search-agent", providerTwo),
        ]);

        var tools = await AgentToolResolver.ResolveAsync(toolProviders, "web-search-agent", cancellationToken);

        Assert.Equal(2, tools.Count);
        Assert.Contains(toolOne, tools);
        Assert.Contains(toolTwo, tools);
    }
}
