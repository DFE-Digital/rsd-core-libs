using Azure.AI.Projects.Agents;
using Azure.Core;
using Azure.Identity;
using GovUK.Dfe.CoreLibs.AiAgents.Agents;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Concurrency;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration.Fakes;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration;

/// <summary><c>ExternallyManagedAgents</c> with an <c>Endpoint</c>: agents in another Foundry project, run there.</summary>
public sealed class ExternalProjectTests
{
    private const string CentralEndpoint = "https://central.services.ai.azure.com/api/projects/agents";
    private static readonly AgentDefinition Ofsted = new("ofsted-agent", "Ofsted");

    private readonly InMemoryFoundry _appFoundry = new();
    private readonly ScriptedConversationClient _appConversations = new();
    private readonly InMemoryFoundry _central = new();
    private readonly ScriptedConversationClient _centralConversations = new();
    private readonly TokenCredential _appCredential = Substitute.For<TokenCredential>();

    private static Dictionary<string, string?> Settings(params (string Key, string Value)[] external)
    {
        var settings = new Dictionary<string, string?>
        {
            ["AiAgents:Foundry:Endpoint"] = "https://app.services.ai.azure.com/api/projects/briefings",
            ["AiAgents:Foundry:DefaultModel"] = "myconnection/gpt-5.1",
        };
        foreach (var (key, value) in external)
        {
            settings[$"AiAgents:ExternallyManagedAgents:{key}"] = value;
        }

        return settings;
    }

    private ServiceProvider Build(Dictionary<string, string?> settings, bool fakeCentralProject = true, Action<AiAgentsBuilder>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddAiAgents(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), agents =>
        {
            agents.UseCredential(_appCredential).AddAgents(Ofsted);
            configure?.Invoke(agents);
        });
        services.AddSingleton(_appFoundry.Admin);
        services.AddSingleton<IFoundryConversationClient>(_appConversations);
        if (fakeCentralProject)
        {
            services.AddSingleton(new ExternalFoundryProject(_appCredential, _central.Admin, _centralConversations,
                new FoundryAgentFactoryOptions("myconnection/gpt-5.1"), new AgentRunOptions(), new AgentRunLimiter(new AgentRunOptions()),
                NullLoggerFactory.Instance));
        }

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task AgentsInAnotherProject_AreResolvedAndRunThere_AtTheirVersion()
    {
        _central.Seed("ofsted-agent", "myconnection/gpt-5.1", "Provisioned centrally.");
        _central.Seed("ofsted-agent", "myconnection/gpt-5.1", "A newer version.");
        _centralConversations.Reply("ofsted-agent", FoundryResponses.Completed("r1", "Rated Good."));
        using var app = Build(Settings(("Endpoint", CentralEndpoint), ("ofsted-agent", "1")));

        var result = await app.GetRequiredService<IAgentService>().RunAsync(Ofsted, "Summarise.");

        Assert.Equal("Rated Good.", result.Output);
        Assert.Equal("1", Assert.Single(_centralConversations.Calls).AgentVersion);
        Assert.Empty(_appConversations.Calls);
        Assert.Empty(_appFoundry.AgentNames);
    }

    [Fact]
    public async Task TheStartupToolCheck_LooksAtTheAgentInItsOwnProject()
    {
        var tools = McpToolClient.BuildFunctionTools("school-performance",
        [
            new ModelContextProtocol.Protocol.Tool
            {
                Name = "get_performance_data",
                InputSchema = System.Text.Json.JsonDocument.Parse("""{"type":"object"}""").RootElement,
            },
        ]);
        _central.Seed("ofsted-agent", "myconnection/gpt-5.1", "Provisioned centrally.", [.. tools]);
        using var app = Build(Settings(("Endpoint", CentralEndpoint), ("ofsted-agent", "1")));
        var check = app.GetServices<IHostedService>().OfType<AgentToolCompatibilityValidator>().Single();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => check.StartAsync(CancellationToken.None));

        Assert.Contains("get_performance_data", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WithItsOwnAuthentication_TheOtherProjectUsesThatServicePrincipal()
    {
        using var app = Build(Settings(("Endpoint", CentralEndpoint), ("ofsted-agent", "1"),
            ("Authentication:TenantId", "central-tenant"), ("Authentication:ClientId", "central-client"),
            ("Authentication:ClientSecret", "central-secret")), fakeCentralProject: false);

        Assert.IsType<ClientSecretCredential>(app.GetRequiredService<ExternalFoundryProject>().Credential);
    }

    [Fact]
    public void WithoutAuthentication_TheOtherProjectUsesThisAppsFoundryCredential()
    {
        using var app = Build(Settings(("Endpoint", CentralEndpoint), ("ofsted-agent", "1")), fakeCentralProject: false);

        Assert.Same(_appCredential, app.GetRequiredService<ExternalFoundryProject>().Credential);
    }

    [Fact]
    public void ACredentialInCode_OverridesTheOthers()
    {
        var central = Substitute.For<TokenCredential>();

        using var app = Build(Settings(("Endpoint", CentralEndpoint), ("ofsted-agent", "1")), fakeCentralProject: false,
            agents => agents.UseExternallyManagedAgentsCredential(central));

        Assert.Same(central, app.GetRequiredService<ExternalFoundryProject>().Credential);
    }

    [Fact]
    public void AuthenticationWithoutAnEndpoint_FailsStartup_BecauseThisAppsOwnProjectAlreadyHasACredential()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Build(Settings(("ofsted-agent", "1"),
            ("Authentication:TenantId", "t"), ("Authentication:ClientId", "c"), ("Authentication:ClientSecret", "s"))));

        Assert.Contains("AiAgents:ExternallyManagedAgents:Endpoint", ex.Message, StringComparison.Ordinal);
    }
}
