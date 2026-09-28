using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using NSubstitute;
using OpenAI.Responses;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Tools.Mcp;

public sealed class McpAllowedToolsProviderTests
{
    private readonly CancellationToken cancellationToken = default;
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

    [Fact]
    public async Task TryExecuteAsync_RunsAnAllowedTool_OnTheClient()
    {
        _client.CallToolAsync("get_performance_data", "{\"urn\":\"100000\"}", Arg.Any<CancellationToken>()).Returns("KS2: 72% expected standard.");
        var sut = new McpAllowedToolsProvider(_client, ["get_performance_data"]);

        var output = await sut.TryExecuteAsync(new ToolCallRequest("call-1", "get_performance_data", "{\"urn\":\"100000\"}"), cancellationToken);

        Assert.Equal("KS2: 72% expected standard.", output);
    }

    [Fact]
    public async Task TryExecuteAsync_ReturnsNull_AndNeverCallsTheServer_ForAToolOutsideItsSubset()
    {
        var sut = new McpAllowedToolsProvider(_client, ["get_performance_data"]);

        var output = await sut.TryExecuteAsync(new ToolCallRequest("call-1", "delete_school", "{}"), cancellationToken);

        Assert.Null(output);
        await _client.DidNotReceiveWithAnyArgs().CallToolAsync(default!, default!, cancellationToken);
    }
}
