using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Tools.Mcp;

public sealed class McpServerConnectionOptionsTests
{
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

    [Fact]
    public void Validate_DoesNotThrow_WhenEveryRequiredFieldIsSet()
    {
        var exception = Record.Exception(() => CreateValidOptions().Validate("my-tools"));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Throws_WhenServerLabelIsEmpty(string serverLabel)
    {
        var options = CreateValidOptions() with { ServerLabel = serverLabel };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate("my-tools"));
        Assert.Contains("ServerLabel", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_Throws_WhenServerUriIsRelative()
    {
        var options = CreateValidOptions() with { ServerUri = new Uri("/relative", UriKind.Relative) };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate("my-tools"));
        Assert.Contains("ServerUri", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_Throws_WhenAuthenticationFieldsAreEmpty()
    {
        var options = CreateValidOptions() with
        {
            Authentication = new McpServerAuthenticationConfig { TenantId = "", ClientId = "", ClientSecret = "", Scope = "" },
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate("my-tools"));
        Assert.Contains("Authentication.TenantId", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Authentication.ClientId", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Authentication.ClientSecret", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Authentication.Scope", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_NamesTheServerKey_InTheExceptionMessage()
    {
        var options = CreateValidOptions() with { ServerLabel = "" };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate("performance-mcp"));
        Assert.Contains("performance-mcp", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ToString_RedactsTheClientSecret()
    {
        var text = CreateValidOptions().ToString();

        Assert.DoesNotContain("secret-1", text, StringComparison.Ordinal);
        Assert.Contains("REDACTED", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthenticationConfig_ToString_RedactsTheClientSecret()
    {
        var text = CreateValidOptions().Authentication.ToString();

        Assert.DoesNotContain("secret-1", text, StringComparison.Ordinal);
        Assert.Contains("REDACTED", text, StringComparison.Ordinal);
    }
}
