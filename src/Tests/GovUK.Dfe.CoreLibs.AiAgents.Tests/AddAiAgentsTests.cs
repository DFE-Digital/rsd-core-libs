using Azure.Core;
using Azure.Identity;
using GovUK.Dfe.CoreLibs.AiAgents.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Concurrency;
using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.Context.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Tools;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests;

public sealed class AddAiAgentsTests
{
    private static Dictionary<string, string?> ValidSettings() => new()
    {
        ["AiAgents:ApplicationName"] = "briefing-tool",
        ["AiAgents:Foundry:Endpoint"] = "https://example.services.ai.azure.com/api/projects/briefings",
        ["AiAgents:Foundry:DefaultModel"] = "my-connection/gpt-4o",
        ["AiAgents:Authentication:TenantId"] = "tenant-1",
        ["AiAgents:Authentication:ClientId"] = "client-1",
        ["AiAgents:Authentication:ClientSecret"] = "secret-1",
        ["AiAgents:RunTimeout"] = "00:02:00",
        ["AiAgents:MaxConcurrency"] = "4",
        ["AiAgents:VersionPins:ofsted-agent"] = "3",
    };

    private static ServiceProvider Build(Dictionary<string, string?> settings, Action<AiAgentsBuilder>? configure = null)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddAiAgents(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), configure);
        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> WithMcpServer(Dictionary<string, string?> settings)
    {
        settings["AiAgents:McpServers:school-performance:ServerUri"] = "https://mcp.internal.example/mcp";
        settings["AiAgents:McpServers:school-performance:Scope"] = "api://school-performance/.default";
        settings["AiAgents:McpServers:school-performance:AllowedToolNames:0"] = "get_performance_data";
        settings["AiAgents:McpServers:school-performance:AllowedToolNames:1"] = "get_absence_data";
        return settings;
    }

    [Fact]
    public void AddAiAgents_IsTheOnlyPublicWayToRegisterTheLibrary()
        => Assert.Equal([nameof(DependencyInjection.AddAiAgents)],
            typeof(DependencyInjection).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Select(method => method.Name).Distinct());

    [Fact]
    public void BindsEachAgentsAllowedMcpTools_ToTheServerThatAllowsThem_AndNothingElse()
    {
        using var provider = Build(WithMcpServer(ValidSettings()), agents => agents.AddAgents(
            new AgentDefinition("ofsted-agent", "Ofsted") { AllowedTools = ["get_performance_data"] },
            new AgentDefinition("synthesis-agent", "Synthesis")));

        var binding = Assert.Single(provider.GetServices<AgentToolBinding>());
        Assert.Equal("ofsted-agent", binding.AgentName);
        Assert.IsType<McpAllowedToolsProvider>(binding.Provider);
        Assert.Equal(2, provider.GetRequiredService<IAgentDefinitionProvider>().GetAgentsDefinitions().Count);
    }

    [Fact]
    public void ExternallyManagedAgents_AreOnlyRun_AtTheirListedVersion_OrTheLatest()
    {
        var settings = ValidSettings();
        settings["AiAgents:ExternallyManagedAgents:trust-agent"] = "4";
        settings["AiAgents:ExternallyManagedAgents:news-agent"] = "latest";

        using var provider = Build(settings);

        var external = provider.GetServices<IManagedAgentProvider>().ToList();
        Assert.Equal(["news-agent", "trust-agent"], external.Select(agent => agent.AgentName).Order());
        Assert.All(external, agent => Assert.False(agent.CreatesAgent));
        var pins = provider.GetRequiredService<AgentVersionPinningOptions>();
        Assert.Equal("4", pins.GetPinnedVersion("trust-agent"));
        Assert.Null(pins.GetPinnedVersion("news-agent"));
        Assert.Equal("3", pins.GetPinnedVersion("ofsted-agent"));   // VersionPins still apply to the app's own agents
    }

    [Fact]
    public void RejectsTheOldListForm_RatherThanRunAnAgentCalledZero()
    {
        var settings = ValidSettings();
        settings["AiAgents:ExternallyManagedAgents:0"] = "trust-agent";

        var ex = Assert.Throws<InvalidOperationException>(() => Build(settings));

        Assert.Contains("not a list", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAnAgentVersionedInBothPlaces()
    {
        var settings = ValidSettings();
        settings["AiAgents:ExternallyManagedAgents:ofsted-agent"] = "4";   // also under VersionPins

        var ex = Assert.Throws<InvalidOperationException>(() => Build(settings));

        Assert.Contains("AiAgents:VersionPins:ofsted-agent", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalConcurrency_UsesBlobLeaseSlots_WithTheConfiguredCapacity()
    {
        var settings = ValidSettings();
        settings["AiAgents:GlobalConcurrency:MaxConcurrentRuns"] = "20";
        settings["AiAgents:GlobalConcurrency:BlobContainerUri"] = "https://account.blob.core.windows.net/run-slots";

        using var provider = Build(settings);

        var slots = Assert.IsType<BlobRunSlotStore>(provider.GetRequiredService<IRunSlotStore>());
        Assert.Equal(20, slots.Capacity);
    }

    [Theory]
    [InlineData("AiAgents:GlobalConcurrency:MaxConcurrentRuns", "20", "AiAgents:GlobalConcurrency:BlobContainerUri")]
    [InlineData("AiAgents:GlobalConcurrency:BlobContainerUri", "https://account.blob.core.windows.net/run-slots", "AiAgents:GlobalConcurrency:MaxConcurrentRuns")]
    [InlineData("AiAgents:MaxEvidenceCharacters", "0", "AiAgents:MaxEvidenceCharacters")]
    [InlineData("AiAgents:AgentCacheDuration", "-00:00:01", "AiAgents:AgentCacheDuration")]
    [InlineData("AiAgents:MaxConcurrency", "0", "AiAgents:MaxConcurrency")]
    public void RejectsIncompleteOrInvalidLimits(string setting, string value, string reported)
    {
        var settings = ValidSettings();
        settings[setting] = value;

        var ex = Assert.Throws<InvalidOperationException>(() => Build(settings));

        Assert.Contains(reported, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MapsEvidenceAndCacheSettings()
    {
        var settings = ValidSettings();
        settings["AiAgents:MaxEvidenceCharacters"] = "50000";
        settings["AiAgents:AgentCacheDuration"] = "00:00:00";

        using var provider = Build(settings);

        Assert.Equal(50_000, provider.GetRequiredService<AgentRunOptions>().MaxEvidenceCharacters);
        Assert.Equal(TimeSpan.Zero, provider.GetRequiredService<FoundryAgentFactoryOptions>().AgentCacheDuration);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("00:00:00", false)]
    public void RunsTheOrphanSweep_UnlessTurnedOff(string? interval, bool expected)
    {
        var settings = ValidSettings();
        if (interval is not null)
        {
            settings["AiAgents:EphemeralAgentSweepInterval"] = interval;
        }

        using var provider = Build(settings);

        Assert.Equal(expected, provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>().OfType<EphemeralAgentSweepService>().Any());
    }

    [Fact]
    public void RejectsTwoDefinitionsWithTheSameName()
        => Assert.Throws<ArgumentException>(() => Build(ValidSettings(), agents => agents.AddAgents(
            new AgentDefinition("ofsted-agent", "Ofsted"), new AgentDefinition("ofsted-agent", "Other"))));

    [Fact]
    public void RegistersTheAgentServiceAndOptions_FromOneConfigurationSection()
    {
        using var provider = Build(ValidSettings());

        Assert.NotNull(provider.GetRequiredService<IAgentService>());
        var runOptions = provider.GetRequiredService<AgentRunOptions>();
        Assert.Equal("briefing-tool", runOptions.ApplicationName);
        Assert.Equal(TimeSpan.FromMinutes(2), runOptions.RunTimeout);
        Assert.Equal(4, runOptions.MaxConcurrency);
        Assert.Equal("3", provider.GetRequiredService<AgentVersionPinningOptions>().GetPinnedVersion("ofsted-agent"));
    }

    [Fact]
    public void ListsEveryMissingSetting_InOneError()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Build(new Dictionary<string, string?>
        {
            ["AiAgents:McpServers:school-performance:ServerUri"] = "https://mcp.internal.example/mcp",
        }));

        foreach (var setting in new[]
                 {
                     "AiAgents:Foundry:Endpoint", "AiAgents:Foundry:DefaultModel", "AiAgents:Authentication:TenantId",
                     "AiAgents:Authentication:ClientId", "AiAgents:Authentication:ClientSecret",
                     "AiAgents:McpServers:school-performance:Scope", "AiAgents:McpServers:school-performance:AllowedToolNames",
                 })
        {
            Assert.Contains(setting, ex.Message, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("-1")]
    public void RejectsKeepingFewerThanTwoVersions(string keepLatestVersions)
    {
        var settings = ValidSettings();
        settings["AiAgents:KeepLatestVersions"] = keepLatestVersions;

        var ex = Assert.Throws<InvalidOperationException>(() => Build(settings));

        Assert.Contains("AiAgents:KeepLatestVersions", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptsACredentialInCode_InsteadOfTheServicePrincipalSecret()
    {
        var settings = ValidSettings();
        settings.Remove("AiAgents:Authentication:ClientSecret");

        using var provider = Build(settings, agents => agents.UseCredential(Substitute.For<TokenCredential>()));

        Assert.NotNull(provider.GetRequiredService<IAgentService>());
    }

    [Fact]
    public void RegistersAzureSearch_WithTheCombinedIndexList_AndTheServicePrincipal()
    {
        var settings = ValidSettings();
        settings["AiAgents:Search:Endpoint"] = "https://example.search.windows.net";
        settings["AiAgents:Search:Indexes:0:Name"] = "ofsted_index";
        settings["AiAgents:Search:Indexes:0:ContentFields:0"] = "title";
        settings["AiAgents:Search:Indexes:0:ContentFields:1"] = "content";
        settings["AiAgents:Search:Indexes:1:Name"] = "news_index";

        using var provider = Build(settings);

        Assert.IsType<AzureSearchContextRetriever>(provider.GetRequiredService<IContextRetriever>());
        var indexes = provider.GetRequiredService<AzureSearchContextRetrieverOptions>().Indexes;
        Assert.Equal(["title", "content"], indexes.Single(index => index.Name == "ofsted_index").ContentFields);
        Assert.Empty(indexes.Single(index => index.Name == "news_index").ContentFields);
    }

    [Fact]
    public void RegistersNoSearch_WhenTheSectionIsAbsent()
    {
        using var provider = Build(ValidSettings());

        Assert.Null(provider.GetService<IContextRetriever>());
    }

    [Fact]
    public void RegistersEachMcpServer_AuthenticatedWithTheServicePrincipal_AndItsOwnScope()
    {
        var settings = ValidSettings();
        settings["AiAgents:McpServers:school-performance:ServerUri"] = "https://mcp.internal.example/mcp";
        settings["AiAgents:McpServers:school-performance:Scope"] = "api://school-performance/.default";
        settings["AiAgents:McpServers:school-performance:AllowedToolNames:0"] = "get_performance_data";

        using var provider = Build(settings);

        Assert.NotNull(provider.GetRequiredKeyedService<IMcpToolClient>("school-performance"));
        var options = provider.GetRequiredKeyedService<McpServerConnectionOptions>("school-performance");
        Assert.Equal(["get_performance_data"], options.AllowedToolNames);
        Assert.Equal("api://school-performance/.default", options.Authentication.Scope);
        Assert.IsType<ClientSecretCredential>(options.Authentication.Credential);
    }
}
