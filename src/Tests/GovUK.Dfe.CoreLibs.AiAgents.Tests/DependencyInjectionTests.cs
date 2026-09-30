using Azure.Core;
using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.Mcp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests;

/// <summary>The internal building blocks <c>AddAiAgents</c> is made of.</summary>
public sealed class DependencyInjectionTests
{
    private sealed class FakeTokenCredential : TokenCredential
    {
        public List<string> RequestedScopes { get; } = [];

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            RequestedScopes.AddRange(requestContext.Scopes);
            return new($"token-for-{requestContext.Scopes[0]}", DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private static McpServerConnectionOptions McpServer(string key, TokenCredential credential) => new()
    {
        ServerLabel = key,
        ServerUri = new Uri($"https://{key}.example.com/mcp"),
        AllowedToolNames = ["get_performance_data"],
        Credential = credential,
        Scope = $"api://{key}/.default",
    };

    private static IConfiguration Search(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => (string?)s.Value)).Build();

    [Fact]
    public void AddAzureSearchContextRetriever_FailsValidation_WhenTwoIndexesShareAName()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddAzureSearchContextRetriever(Search(
            ("Endpoint", "https://example.search.windows.net"),
            ("Indexes:0:Name", "establishment_index"),
            ("Indexes:1:Name", "establishment_index")), new FakeTokenCredential());

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<AzureSearchContextRetrieverOptions>>().Value);
        Assert.Contains("unique Name", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddMcpClientServices_EveryMcpRequest_CarriesATokenTheLibraryGotFromTheAppsCredential_ForThatServersScope()
    {
        var credential = new FakeTokenCredential();
        var server = new CapturingHandler();
        var services = new ServiceCollection().AddLogging();
        services.AddMcpClientServices("school-performance", McpServer("school-performance", credential));
        services.AddHttpClient($"{McpToolClient.HttpClientName}:school-performance").ConfigurePrimaryHttpMessageHandler(() => server);

        await using var provider = services.BuildServiceProvider();
        using var http = provider.GetRequiredService<IHttpClientFactory>().CreateClient($"{McpToolClient.HttpClientName}:school-performance");
        await http.GetAsync(new Uri("https://school-performance.example.com/mcp"));
        await http.GetAsync(new Uri("https://school-performance.example.com/mcp"));

        Assert.Equal("Bearer token-for-api://school-performance/.default", server.Authorization);
        Assert.Equal(["api://school-performance/.default"], credential.RequestedScopes);   // fetched once, then cached
    }
}
