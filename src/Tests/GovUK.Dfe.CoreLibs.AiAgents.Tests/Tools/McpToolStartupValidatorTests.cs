using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenAI.Responses;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Tools;

public sealed class McpToolStartupValidatorTests
{
    private readonly CancellationToken cancellationToken = default;
    private readonly IMcpToolClient _client = Substitute.For<IMcpToolClient>();
    private readonly ILogger<McpToolStartupValidator> _logger = Substitute.For<ILogger<McpToolStartupValidator>>();

    private static McpServerConnectionOptions CreateValidOptions() => new()
    {
        ServerLabel = "my-tools",
        ServerUri = new Uri("https://mcp.example.com"),
        Authentication = new McpServerAuthenticationConfig
        {
            TenantId = "tenant-1",
            ClientId = "client-1",
            ClientSecret = "secret-1",
            Scope = "api://mcp/.default",
        },
    };

    private McpToolStartupValidator CreateSut(McpServerConnectionOptions? options = null)
        => new("my-tools", _client, options ?? CreateValidOptions(), _logger);

    [Fact]
    public async Task StartAsync_CallsGetToolsAsync_ToValidateConfiguration()
    {
        _client.GetToolsAsync(cancellationToken).Returns(Task.FromResult<IReadOnlyList<ResponseTool>>([]));
        var sut = CreateSut();

        await sut.StartAsync(cancellationToken);

        await _client.Received(1).GetToolsAsync(cancellationToken);
    }

    [Fact]
    public async Task StartAsync_Propagates_WhenValidationFails()
    {
        var exception = new InvalidOperationException("MCP server 'foo' does not expose the following configured tool(s): bar.");
        _client.GetToolsAsync(cancellationToken).Throws(exception);
        var sut = CreateSut();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.StartAsync(cancellationToken));
        Assert.Same(exception, thrown.InnerException);
    }

    [Fact]
    public async Task StartAsync_ThrowsAndNeverConnects_WhenOptionsAreInvalid()
    {
        var invalidOptions = CreateValidOptions() with { ServerLabel = "" };
        var sut = CreateSut(invalidOptions);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.StartAsync(cancellationToken));

        await _client.DidNotReceiveWithAnyArgs().GetToolsAsync(cancellationToken);
    }

    [Fact]
    public async Task StopAsync_CompletesWithoutError()
    {
        var sut = CreateSut();

        var exception = await Record.ExceptionAsync(() => sut.StopAsync(cancellationToken));

        Assert.Null(exception);
    }
}
