using Azure.Core;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Tools.Mcp;

public sealed class McpServerConnectionOptionsTests
{
    private static McpServerConnectionOptions ValidOptions() => new()
    {
        ServerLabel = "my-tools",
        ServerUri = new Uri("https://mcp.example.com"),
        AllowedToolNames = ["get_performance_data"],
        Credential = Substitute.For<TokenCredential>(),
        Scope = "api://mcp/.default",
    };

    [Fact]
    public void Validate_NamesEveryProblem_AndTheServer_InOneError()
    {
        var options = ValidOptions() with
        {
            ServerLabel = " ",
            ServerUri = new Uri("/relative", UriKind.Relative),
            AllowedToolNames = [],
            Scope = "",
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate("performance-mcp"));

        foreach (var expected in new[] { "performance-mcp", "ServerLabel", "ServerUri", "AllowedToolNames", "Scope" })
        {
            Assert.Contains(expected, ex.Message, StringComparison.Ordinal);
        }
    }
}
