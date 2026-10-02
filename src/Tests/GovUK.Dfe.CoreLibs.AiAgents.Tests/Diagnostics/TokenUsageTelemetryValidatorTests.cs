using Azure.Core;
using GovUK.Dfe.CoreLibs.AiAgents.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Diagnostics;

public sealed class TokenUsageTelemetryValidatorTests
{
    private readonly CancellationToken cancellationToken = default;

    [Fact]
    public async Task StartedAsync_FailsStartup_WithTheFix_WhenTokenUsageIsntRecorded()
    {
        var sut = new TokenUsageTelemetryValidator(required: true, () => false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.StartedAsync(cancellationToken));

        Assert.Contains("AddMeter(AgentTelemetry.SourceName)", ex.Message, StringComparison.Ordinal);
        Assert.Contains("RequireTokenUsageTelemetry to false", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartedAsync_Passes_WhenTokenUsageIsRecorded()
    {
        var sut = new TokenUsageTelemetryValidator(required: true, () => true);

        Assert.Null(await Record.ExceptionAsync(() => sut.StartedAsync(cancellationToken)));
    }

    [Fact]
    public async Task StartAsync_NeverChecks_SoItRunsAfterOpenTelemetryHasAttachedItsListener()
    {
        var checkedEarly = false;
        var sut = new TokenUsageTelemetryValidator(required: true, () => checkedEarly = true);

        await sut.StartingAsync(cancellationToken);
        await sut.StartAsync(cancellationToken);

        Assert.False(checkedEarly);
    }

    [Fact]
    public async Task StartedAsync_DoesNothing_WhenTheRequirementIsTurnedOff()
    {
        var checkedAtAll = false;
        var sut = new TokenUsageTelemetryValidator(required: false, () => checkedAtAll = true);

        Assert.Null(await Record.ExceptionAsync(() => sut.StartedAsync(cancellationToken)));
        Assert.False(checkedAtAll);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAiAgents_AlwaysRegistersTheCheck_CarryingTheSetting(bool required)
    {
        var services = new ServiceCollection();

        services.AddAiAgents(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiAgents:Foundry:Endpoint"] = "https://example.services.ai.azure.com/api/projects/test",
                ["AiAgents:Foundry:DefaultModel"] = "gpt-4o",
                ["AiAgents:RequireTokenUsageTelemetry"] = required.ToString(),
            }).Build(),
            agents => agents.UseCredential(Substitute.For<TokenCredential>()));

        using var provider = services.BuildServiceProvider();
        Assert.Single(provider.GetServices<IHostedService>().OfType<TokenUsageTelemetryValidator>());
        Assert.Equal(required, provider.GetRequiredService<AgentRunOptions>().RequireTokenUsageTelemetry);
    }

    [Fact]
    public void RequireTokenUsageTelemetry_IsOnByDefault()
        => Assert.True(new AiAgentsOptions().RequireTokenUsageTelemetry);
}
