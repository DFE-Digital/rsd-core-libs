using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using OpenAI.Responses;
using System.Text.Json;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Tools.Mcp;

/// <summary>
/// McpToolClient against a fake connection, so connection failures and reconnects can be tested
/// without a real MCP server.
/// </summary>
public sealed class McpToolClientTests
{
    private readonly CancellationToken cancellationToken = default;

    private sealed class FakeSession(Func<int, IReadOnlyList<Tool>> listTools, Func<string, CallToolResult>? callTool = null) : IMcpSession
    {
        public int ListCalls { get; private set; }
        public int ToolCalls { get; private set; }
        public bool Disposed { get; private set; }

        public Task<IReadOnlyList<Tool>> ListToolsAsync(CancellationToken cancellationToken) => Task.FromResult(listTools(++ListCalls));

        public Task<CallToolResult> CallToolAsync(string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
        {
            ToolCalls++;
            return Task.FromResult(callTool?.Invoke(toolName) ?? new CallToolResult { Content = [new TextContentBlock { Text = $"{toolName} ok" }] });
        }

        public Task<GetPromptResult> GetPromptAsync(string name, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
            => Task.FromResult(new GetPromptResult { Messages = [] });

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private static Tool ServerTool(string name) => new()
    {
        Name = name,
        InputSchema = JsonDocument.Parse("""{"type":"object"}""").RootElement,
    };

    private static readonly IReadOnlyList<Tool> ServerTools = [ServerTool("get_performance_data"), ServerTool("update_school_record")];

    private static McpServerConnectionOptions Options(params string[] allowed) => new()
    {
        ServerLabel = "school-performance-mcp",
        ServerUri = new Uri("https://mcp.example.gov.uk/mcp"),
        AllowedToolNames = allowed,
        ToolListCacheDuration = TimeSpan.Zero,
        Credential = NSubstitute.Substitute.For<Azure.Core.TokenCredential>(),
        Scope = "api://mcp/.default",
    };

    [Fact]
    public async Task AFailedConnection_IsReplaced_AndSafeOperationsRetryOnTheNewOne()
    {
        var broken = new FakeSession(_ => throw new HttpRequestException("Session expired."));
        var healthy = new FakeSession(_ => ServerTools);
        var sessions = new Queue<IMcpSession>([broken, healthy]);
        var sut = new McpToolClient(Options("get_performance_data"), NullLogger<McpToolClient>.Instance, _ => Task.FromResult(sessions.Dequeue()));

        var tools = await sut.GetToolsAsync(cancellationToken);

        Assert.Equal("get_performance_data", Assert.IsType<FunctionTool>(Assert.Single(tools)).FunctionName);
        Assert.True(broken.Disposed);
        Assert.Equal(1, healthy.ListCalls);
    }

    [Fact]
    public async Task AFailedConnect_IsNotRemembered_SoTheNextCallTriesAgain()
    {
        var attempts = 0;
        var sut = new McpToolClient(Options("get_performance_data"), NullLogger<McpToolClient>.Instance, _ =>
            ++attempts == 1
                ? Task.FromException<IMcpSession>(new HttpRequestException("Server restarting."))
                : Task.FromResult<IMcpSession>(new FakeSession(_ => ServerTools)));

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetToolsAsync(cancellationToken));
        var tools = await sut.GetToolsAsync(cancellationToken);

        Assert.Single(tools);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task AFailedToolCall_DropsTheConnection_ButIsNotRetried_SoItsSideEffectsArentRepeated()
    {
        var failing = new FakeSession(_ => ServerTools, _ => throw new HttpRequestException("Connection reset."));
        var replacement = new FakeSession(_ => ServerTools);
        var sessions = new Queue<IMcpSession>([failing, replacement]);
        var sut = new McpToolClient(Options("update_school_record"), NullLogger<McpToolClient>.Instance, _ => Task.FromResult(sessions.Dequeue()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CallToolAsync("update_school_record", "{}", cancellationToken));

        Assert.Equal(1, failing.ToolCalls);
        Assert.True(failing.Disposed);
        Assert.Equal("update_school_record ok", await sut.CallToolAsync("update_school_record", "{}", cancellationToken));
    }

    [Fact]
    public async Task AToolOutsideAllowedToolNames_IsNeverDescribedOrRun_EvenThoughTheServerHasIt()
    {
        var session = new FakeSession(_ => ServerTools);
        var sut = new McpToolClient(Options("get_performance_data"), NullLogger<McpToolClient>.Instance, _ => Task.FromResult<IMcpSession>(session));

        var described = await sut.GetToolsAsync(cancellationToken);
        var viaExecutor = await sut.TryExecuteAsync(new ToolCallRequest("call-1", "update_school_record", "{}"), cancellationToken);

        Assert.DoesNotContain(described.OfType<FunctionTool>(), tool => tool.FunctionName == "update_school_record");
        Assert.Null(viaExecutor);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CallToolAsync("update_school_record", "{}", cancellationToken));
        await Assert.ThrowsAsync<McpToolConfigurationException>(() => sut.GetToolsAsync(["update_school_record"], cancellationToken));
        Assert.Equal(0, session.ToolCalls);
    }

    [Fact]
    public async Task AToolReportedError_GoesBackToTheModel()
    {
        var session = new FakeSession(_ => ServerTools, _ => new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = "URN 999999 not found." }],
        });
        var sut = new McpToolClient(Options("get_performance_data"), NullLogger<McpToolClient>.Instance, _ => Task.FromResult<IMcpSession>(session));

        var output = await sut.CallToolAsync("get_performance_data", "{\"urn\":\"999999\"}", cancellationToken);

        Assert.Equal("The tool reported an error: URN 999999 not found.", output);
    }

    [Theory]
    [InlineData("{\"urn\":")]       // truncated
    [InlineData("[\"100000\"]")]    // not an object
    public async Task MalformedArguments_GoBackToTheModel_WithoutCallingTheServerOrDroppingTheConnection(string arguments)
    {
        var session = new FakeSession(_ => ServerTools);
        var connects = 0;
        var sut = new McpToolClient(Options("get_performance_data"), NullLogger<McpToolClient>.Instance, _ =>
        {
            connects++;
            return Task.FromResult<IMcpSession>(session);
        });

        var output = await sut.CallToolAsync("get_performance_data", arguments, cancellationToken);
        await sut.CallToolAsync("get_performance_data", "{\"urn\":\"100000\"}", cancellationToken);

        Assert.Contains("valid JSON object", output, StringComparison.Ordinal);
        Assert.Equal(1, session.ToolCalls);
        Assert.Equal(1, connects);
        Assert.False(session.Disposed);
    }

    [Fact]
    public void Validate_RequiresAllowedToolNames()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Options().Validate("school-performance-mcp"));

        Assert.Contains("AllowedToolNames", ex.Message, StringComparison.Ordinal);
    }
}
