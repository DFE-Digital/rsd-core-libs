using Azure.Identity;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Tools.WebSearch;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration;

/// <summary>
/// Runs only when <c>AIAGENTS_LIVE_FOUNDRY_ENDPOINT</c> is set. Checks what the in-memory fakes can't:
/// that the real Foundry service returns an agent definition exactly as it was sent, so an unchanged
/// spec reuses its version instead of creating a new one on every call.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LiveFoundryFactAttribute : FactAttribute
{
    public LiveFoundryFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(LiveFoundrySmokeTests.EndpointVariable)))
        {
            Skip = $"Set {LiveFoundrySmokeTests.EndpointVariable} (and optionally {LiveFoundrySmokeTests.ModelVariable}) to run against a real Foundry project.";
        }
    }
}

public sealed class LiveFoundrySmokeTests
{
    public const string EndpointVariable = "AIAGENTS_LIVE_FOUNDRY_ENDPOINT";
    public const string ModelVariable = "AIAGENTS_LIVE_FOUNDRY_MODEL";

    private static ServiceProvider Build() => new ServiceCollection()
        .AddLogging()
        .AddFoundryAgents(
            endpoint: _ => new Uri(Environment.GetEnvironmentVariable(EndpointVariable)!),
            credential: _ => new DefaultAzureCredential(),
            // No cache, so the second call really asks Foundry whether the version matches.
            optionsFactory: _ => new FoundryAgentFactoryOptions(Environment.GetEnvironmentVariable(ModelVariable) ?? "gpt-4o")
            {
                AgentCacheDuration = TimeSpan.Zero,
            })
        .BuildServiceProvider();

    private static string UniqueName(string purpose) => $"aiagents-smoke-{purpose}-{DateTime.UtcNow:yyyyMMddHHmmss}";

    [LiveFoundryFact]
    public async Task UnchangedSpec_ReusesItsVersion_AndRuns()
    {
        using var provider = Build();
        var factory = provider.GetRequiredService<IAgentFactory>();
        var spec = new AgentSpec { Name = UniqueName("reuse"), Instructions = "Reply with the single word: ready." };

        try
        {
            var first = await factory.GetOrCreateAsync(spec);
            var second = await factory.GetOrCreateAsync(spec);
            Assert.Equal(first.Version, second.Version);

            var result = await provider.GetRequiredService<IAgentRunner>().RunAsync(second, "Are you ready?");
            Assert.False(string.IsNullOrWhiteSpace(result.Output));
            Assert.True(result.TotalTokens > 0);
        }
        finally
        {
            await factory.DeleteAgentAsync(spec.Name);
        }
    }

    [LiveFoundryFact]
    public async Task UnchangedSpecWithATool_ReusesItsVersion()
    {
        using var provider = Build();
        var factory = provider.GetRequiredService<IAgentFactory>();
        var spec = new AgentSpec
        {
            Name = UniqueName("tools"),
            Instructions = "You search the web.",
            Tools = await new WebSearchToolProvider().GetToolsAsync(),
        };

        try
        {
            var first = await factory.GetOrCreateAsync(spec);
            var second = await factory.GetOrCreateAsync(spec);
            Assert.Equal(first.Version, second.Version);
        }
        finally
        {
            await factory.DeleteAgentAsync(spec.Name);
        }
    }
}
