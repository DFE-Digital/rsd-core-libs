using GovUK.Dfe.CoreLibs.AiAgents.Context;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Context;

public sealed class AzureSearchContextRetrieverOptionsTests
{
    [Fact]
    public void ToString_RedactsTheClientSecret()
    {
        var options = new AzureSearchContextRetrieverOptions
        {
            Endpoint = "https://example.search.windows.net",
            TenantId = "tenant-1",
            ClientId = "client-1",
            ClientSecret = "super-secret-value",
            Indexes = ["establishment-index"],
        };

        var text = options.ToString();

        Assert.DoesNotContain("super-secret-value", text, StringComparison.Ordinal);
        Assert.Contains("REDACTED", text, StringComparison.Ordinal);
    }
}
