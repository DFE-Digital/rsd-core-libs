using Azure.AI.Projects;
using Azure.Core;
using GovUK.Dfe.CoreLibs.AiAgents.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.Context.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration;
using GovUK.Dfe.CoreLibs.AiAgents.Orchestration.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Prompts.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests;

public sealed class DependencyInjectionTests
{
    private sealed class FakeTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => new("fake-token", DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }

    [Fact]
    public void AddFoundryAgents_WithEndpointAndCredential_RegistersAllExpectedServices()
    {
        var services = new ServiceCollection();

        services.AddFoundryAgents(
            endpoint: _ => new Uri("https://example.services.ai.azure.com/api/projects/test"),
            credential: _ => new FakeTokenCredential(),
            optionsFactory: _ => new FoundryAgentFactoryOptions("gpt-4o"));

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<AIProjectClient>());
        Assert.IsType<FoundryAgentFactory>(provider.GetRequiredService<IAgentFactory>());
        Assert.IsType<FoundryConversationClient>(provider.GetRequiredService<IFoundryConversationClient>());
        Assert.IsType<FoundryAgentRunner>(provider.GetRequiredService<IAgentRunner>());
        Assert.IsType<AgentOrchestrator>(provider.GetRequiredService<IAgentOrchestrator>()); 
        Assert.IsType<AgentRuntime>(provider.GetRequiredService<IAgentRuntime>());
    }

    [Fact]
    public void AddFoundryAgents_AgentRuntimeResolveAsync_UsesThePinnedVersion_WhenAddAgentVersionPinningIsCalled()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Agents:VersionPins:my-agent"] = "3",
            })
            .Build();

        services.AddFoundryAgents(
            endpoint: _ => new Uri("https://example.services.ai.azure.com/api/projects/test"),
            credential: _ => new FakeTokenCredential(),
            optionsFactory: _ => new FoundryAgentFactoryOptions("gpt-4o"));
        services.AddAgentVersionPinning(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.IsType<AgentRuntime>(provider.GetRequiredService<IAgentRuntime>());
        Assert.Equal("3", provider.GetRequiredService<AgentVersionPinningOptions>().GetPinnedVersion("my-agent"));
    }

    [Fact]
    public void AddAzureSearchContextRetriever_RegistersRealRetrieverAndFilter_FromValidConfiguration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureSearch:Endpoint"] = "https://example.search.windows.net",
                ["AzureSearch:TenantId"] = "tenant-1",
                ["AzureSearch:ClientId"] = "client-1",
                ["AzureSearch:ClientSecret"] = "secret-1",
                ["AzureSearch:Indexes:0:Name"] = "establishment-index",
                ["AzureSearch:Indexes:1:Name"] = "ofsted-index",
            })
            .Build();

        services.AddAzureSearchContextRetriever(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.IsType<RelativeScoreRelevanceFilter>(provider.GetRequiredService<IRelevanceFilter>());
        Assert.IsType<AzureSearchContextRetriever>(provider.GetRequiredService<IContextRetriever>());
    }

    [Fact]
    public void AddAzureSearchContextRetriever_ThrowsOnResolve_WhenARequiredConfigValueIsMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Endpoint deliberately omitted - AzureSearchContextRetrieverOptions.Endpoint is `required`.
                ["AzureSearch:TenantId"] = "tenant-1",
                ["AzureSearch:ClientId"] = "client-1",
                ["AzureSearch:ClientSecret"] = "secret-1",
                ["AzureSearch:Indexes:0:Name"] = "establishment-index",
            })
            .Build();

        services.AddAzureSearchContextRetriever(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.ThrowsAny<Exception>(() => provider.GetRequiredService<IContextRetriever>());
    }

    [Fact]
    public void AddAzureSearchContextRetriever_UsesTheGivenCredential_WithoutAnyClientSecretConfiguration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureSearch:Endpoint"] = "https://example.search.windows.net",
                ["AzureSearch:Indexes:0:Name"] = "establishment-index",
                ["AzureSearch:Indexes:0:ContentFields:0"] = "name",
                ["AzureSearch:Indexes:0:ContentFields:1"] = "summary",
            })
            .Build();
        var credentialUsed = false;

        services.AddAzureSearchContextRetriever(configuration, _ =>
        {
            credentialUsed = true;
            return new FakeTokenCredential();
        });

        using var provider = services.BuildServiceProvider();

        Assert.IsType<AzureSearchContextRetriever>(provider.GetRequiredService<IContextRetriever>());
        Assert.True(credentialUsed);
        Assert.Equal(["name", "summary"], provider.GetRequiredService<AzureSearchContextRetrieverOptions>().Indexes[0].ContentFields);
    }

    [Fact]
    public void AddAzureSearchContextRetriever_FailsValidation_WhenTwoIndexesShareAName()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureSearch:Endpoint"] = "https://example.search.windows.net",
                ["AzureSearch:Indexes:0:Name"] = "establishment_index",
                ["AzureSearch:Indexes:1:Name"] = "establishment_index",
            })
            .Build();

        services.AddAzureSearchContextRetriever(configuration, _ => new FakeTokenCredential());
        using var provider = services.BuildServiceProvider();

        // The same validation ValidateOnStart runs when the host starts.
        var ex = Assert.Throws<Microsoft.Extensions.Options.OptionsValidationException>(
            () => provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AzureSearchContextRetrieverOptions>>().Value);
        Assert.Contains("unique Name", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddAzureSearchContextRetriever_ExplainsTheFix_WhenThereIsNeitherACredentialNorAClientSecret()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureSearch:Endpoint"] = "https://example.search.windows.net",
                ["AzureSearch:Indexes:0:Name"] = "establishment-index",
            })
            .Build();

        services.AddAzureSearchContextRetriever(configuration);

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IContextRetriever>());
        Assert.Contains("credential", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddAgentExecution_RegistersRunOptions_FromAgentExecutionOptions()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddAgentExecution(configuration,
            endpoint: _ => new Uri("https://example.services.ai.azure.com/api/projects/test"),
            credential: _ => new FakeTokenCredential(),
            optionsFactory: _ => new FoundryAgentFactoryOptions("gpt-4o"),
            agentExecutionOptions: new AgentExecutionOptions
            {
                RunTimeout = TimeSpan.FromMinutes(2), MaxConcurrency = 4, DeleteConversationsAfterRun = false,
            });

        using var provider = services.BuildServiceProvider();

        Assert.Equal(new AgentRunOptions { RunTimeout = TimeSpan.FromMinutes(2), MaxConcurrency = 4, DeleteConversationsAfterRun = false },
            provider.GetRequiredService<AgentRunOptions>());
    }

    [Fact]
    public void AddAgentVersionPinning_BindsPinnedVersions_FromConfiguration()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Agents:VersionPins:establishment-agent"] = "3",
                ["Agents:VersionPins:ofsted-agent"] = "7",
            })
            .Build();

        services.AddAgentVersionPinning(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<AgentVersionPinningOptions>();

        Assert.Equal("3", options.GetPinnedVersion("establishment-agent"));
        Assert.Equal("7", options.GetPinnedVersion("ofsted-agent"));
        Assert.Null(options.GetPinnedVersion("unpinned-agent"));
    }

    [Fact]
    public async Task AddMcpClientServices_RegistersAllExpectedServices_KeyedByServerKey()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMcpClientServices("my-tools", _ => new McpServerConnectionOptions
        {
            ServerLabel = "my-tools",
            ServerUri = new Uri("https://mcp.example.com"),
            AllowedToolNames = ["get_performance_data"],
            Authentication = new McpServerAuthenticationConfig
            {
                TenantId = "tenant-1",
                ClientId = "client-1",
                ClientSecret = "secret-1",
                Scope = "api://mcp/.default",
            },
        });

        await using var provider = services.BuildServiceProvider();

        Assert.IsType<TokenService>(provider.GetRequiredKeyedService<ITokenService>("my-tools"));
        Assert.IsType<McpToolClient>(provider.GetRequiredKeyedService<IMcpToolClient>("my-tools"));
        Assert.Contains(provider.GetServices<IHostedService>(), s => s is McpToolStartupValidator);
    }

    [Fact]
    public async Task AddMcpClientServices_CalledTwice_RegistersTwoIndependentlyResolvableServers()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMcpClientServices("server-a", _ => new McpServerConnectionOptions
        {
            ServerLabel = "server-a",
            ServerUri = new Uri("https://mcp-a.example.com"), AllowedToolNames = ["get_performance_data"],
            Authentication = new McpServerAuthenticationConfig
            {
                TenantId = "tenant-a", ClientId = "client-a", ClientSecret = "secret-a", Scope = "api://mcp-a/.default",
            },
        });
        services.AddMcpClientServices("server-b", _ => new McpServerConnectionOptions
        {
            ServerLabel = "server-b",
            ServerUri = new Uri("https://mcp-b.example.com"), AllowedToolNames = ["get_performance_data"],
            Authentication = new McpServerAuthenticationConfig
            {
                TenantId = "tenant-b", ClientId = "client-b", ClientSecret = "secret-b", Scope = "api://mcp-b/.default",
            },
        });

        await using var provider = services.BuildServiceProvider();

        var clientA = provider.GetRequiredKeyedService<IMcpToolClient>("server-a");
        var clientB = provider.GetRequiredKeyedService<IMcpToolClient>("server-b");

        Assert.NotSame(clientA, clientB);
        Assert.Equal(2, provider.GetServices<IHostedService>().Count(s => s is McpToolStartupValidator));
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void AddAgentExecution_RegistersFoundryAgentsPromptsAndVersionPinning_ByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = BuildConfiguration(new() { ["Agents:VersionPins:my-agent"] = "3" });

        services.AddAgentExecution(configuration,
            endpoint: _ => new Uri("https://example.services.ai.azure.com/api/projects/test"),
            credential: _ => new FakeTokenCredential(),
            optionsFactory: _ => new FoundryAgentFactoryOptions("gpt-4o"));

        using var provider = services.BuildServiceProvider();

        Assert.IsType<FoundryAgentFactory>(provider.GetRequiredService<IAgentFactory>());
        Assert.IsType<AgentRuntime>(provider.GetRequiredService<IAgentRuntime>());
        Assert.IsType<AgentService>(provider.GetRequiredService<IAgentService>());
        Assert.NotNull(provider.GetRequiredService<IPromptProvider>());
        Assert.Equal("3", provider.GetRequiredService<AgentVersionPinningOptions>().GetPinnedVersion("my-agent"));
        Assert.DoesNotContain(provider.GetServices<IHostedService>(), s => s is PinnedAgentVersionDriftValidator);
    }

    [Fact]
    public void AddAgentExecution_SkipsVersionPinning_WhenDisabled()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration([]);

        services.AddAgentExecution(configuration,
            endpoint: _ => new Uri("https://example.services.ai.azure.com/api/projects/test"),
            credential: _ => new FakeTokenCredential(),
            optionsFactory: _ => new FoundryAgentFactoryOptions("gpt-4o"),
            agentExecutionOptions: new AgentExecutionOptions { EnableVersionPinning = false });

        using var provider = services.BuildServiceProvider();

        Assert.Null(provider.GetService<AgentVersionPinningOptions>());
    }

    [Fact]
    public void AddAgentExecution_RegistersDriftDetection_WhenEnabled_AndAnIAgentDefinitionProviderIsAlsoRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = BuildConfiguration([]);

        services.AddAgentExecution(configuration,
            endpoint: _ => new Uri("https://example.services.ai.azure.com/api/projects/test"),
            credential: _ => new FakeTokenCredential(),
            optionsFactory: _ => new FoundryAgentFactoryOptions("gpt-4o"),
            agentExecutionOptions: new AgentExecutionOptions { EnableDriftDetection = true });
        services.AddSingleton<IAgentDefinitionProvider>(new Integration.Fakes.FakeAgentDefinitions());   // AddAiAgents registers this from AddAgents

        using var provider = services.BuildServiceProvider();

        Assert.Contains(provider.GetServices<IHostedService>(), s => s is PinnedAgentVersionDriftValidator);
    }

    [Fact]
    public void AddAgentContextAndTools_RegistersAzureSearch_WhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = BuildConfiguration(new()
        {
            ["AzureSearch:Endpoint"] = "https://example.search.windows.net",
            ["AzureSearch:TenantId"] = "tenant-1",
            ["AzureSearch:ClientId"] = "client-1",
            ["AzureSearch:ClientSecret"] = "secret-1",
            ["AzureSearch:Indexes:0:Name"] = "establishment-index",
        });

        services.AddAgentContextAndTools(configuration, enableAzureSearch: true);

        using var provider = services.BuildServiceProvider();

        Assert.IsType<AzureSearchContextRetriever>(provider.GetRequiredService<IContextRetriever>());
    }

    [Fact]
    public async Task AddAgentContextAndTools_RegistersEachMcpServer_KeyedByItsOwnServerKey()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = BuildConfiguration([]);

        services.AddAgentContextAndTools(configuration, mcpServers:
        [
            new McpServerRegistration("server-a", _ => new McpServerConnectionOptions
            {
                ServerLabel = "server-a", ServerUri = new Uri("https://mcp-a.example.com"), AllowedToolNames = ["get_performance_data"],
                Authentication = new McpServerAuthenticationConfig
                {
                    TenantId = "tenant-a", ClientId = "client-a", ClientSecret = "secret-a", Scope = "api://mcp-a/.default",
                },
            }),
            new McpServerRegistration("server-b", _ => new McpServerConnectionOptions
            {
                ServerLabel = "server-b", ServerUri = new Uri("https://mcp-b.example.com"), AllowedToolNames = ["get_performance_data"],
                Authentication = new McpServerAuthenticationConfig
                {
                    TenantId = "tenant-b", ClientId = "client-b", ClientSecret = "secret-b", Scope = "api://mcp-b/.default",
                },
            }),
        ]);

        await using var provider = services.BuildServiceProvider();

        Assert.IsType<McpToolClient>(provider.GetRequiredKeyedService<IMcpToolClient>("server-a"));
        Assert.IsType<McpToolClient>(provider.GetRequiredKeyedService<IMcpToolClient>("server-b"));
        Assert.Equal(2, provider.GetServices<IHostedService>().Count(s => s is McpToolStartupValidator));
    }
}
