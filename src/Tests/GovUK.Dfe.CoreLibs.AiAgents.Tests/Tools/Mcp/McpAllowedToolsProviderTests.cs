using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using NSubstitute;
using OpenAI.Responses;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Tools.Mcp;

public sealed class McpAllowedToolsProviderTests
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;
    private readonly IMcpToolClient _client = Substitute.For<IMcpToolClient>();

    [Fact]
    public async Task GetToolsAsync_ForwardsItsOwnAllowedToolNames_ToTheClient_NotTheClientsConnectionLevelDefault()
    {
        var mcpTool = ResponseTool.CreateMcpTool(
            serverLabel: "my-tools", serverUri: new Uri("https://mcp.example.com"), serverDescription: null,
            allowedTools: null, toolCallApprovalPolicy: null);
        _client.GetToolsAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ResponseTool>>([mcpTool]));

        var sut = new McpAllowedToolsProvider(_client, ["get_performance_data"]);
        var tools = await sut.GetToolsAsync(cancellationToken);

        var expectedNames = new[] { "get_performance_data" };
        Assert.Same(mcpTool, Assert.Single(tools));
        await _client.Received(1).GetToolsAsync(
            Arg.Is<IReadOnlyList<string>?>(names => names != null && names.SequenceEqual(expectedNames)),
            cancellationToken);
    }

    [Fact]
    public async Task GetToolsAsync_DifferentInstances_CanRestrictTheSameClient_ToDifferentSubsets()
    {
        var toolA = ResponseTool.CreateMcpTool(
            serverLabel: "my-tools", serverUri: new Uri("https://mcp.example.com"), serverDescription: null,
            allowedTools: null, toolCallApprovalPolicy: null);
        var toolB = ResponseTool.CreateMcpTool(
            serverLabel: "my-tools", serverUri: new Uri("https://mcp.example.com"), serverDescription: null,
            allowedTools: null, toolCallApprovalPolicy: null);

        _client.GetToolsAsync(Arg.Is<IReadOnlyList<string>?>(names => names != null && names.Contains("tool-a")), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ResponseTool>>([toolA]));
        _client.GetToolsAsync(Arg.Is<IReadOnlyList<string>?>(names => names != null && names.Contains("tool-b")), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ResponseTool>>([toolB]));

        var providerForAgentA = new McpAllowedToolsProvider(_client, ["tool-a"]);
        var providerForAgentB = new McpAllowedToolsProvider(_client, ["tool-b"]);

        Assert.Same(toolA, Assert.Single(await providerForAgentA.GetToolsAsync(cancellationToken)));
        Assert.Same(toolB, Assert.Single(await providerForAgentB.GetToolsAsync(cancellationToken)));
    }
}
