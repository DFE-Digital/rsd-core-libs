using Azure.AI.Projects.Agents;
using Azure.Core;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Context;
using GovUK.Dfe.CoreLibs.AiAgents.Diagnostics;
using GovUK.Dfe.CoreLibs.AiAgents.Extensions;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration.Fakes;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration;

/// <summary>
/// Listens to the library's real ActivitySource and Meter while agents run through its DI wiring.
/// Each test uses its own application name, so measurements from tests running in parallel never mix.
/// </summary>
public sealed class AgentTelemetryTests : IDisposable
{
    private sealed record Measurement(string Instrument, double Value, IReadOnlyDictionary<string, object?> Tags);

    private readonly string _application = $"telemetry-test-{Guid.NewGuid():N}";
    private readonly string _promptDirectory = Directory.CreateTempSubdirectory("aiagents-telemetry-").FullName;
    private readonly InMemoryFoundry _foundry = new();
    private readonly ScriptedConversationClient _conversations = new();
    private readonly Dictionary<string, string?> _configuration = [];
    private readonly ConcurrentQueue<Measurement> _measurements = new();
    private readonly ConcurrentQueue<Activity> _activities = new();
    private readonly MeterListener _meterListener = new();
    private readonly ActivityListener _activityListener;

    public AgentTelemetryTests()
    {
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AgentTelemetry.SourceName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument, value, tags));
        _meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument, value, tags));
        _meterListener.Start();

        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AgentTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (Equals(activity.GetTagItem(AgentTelemetry.ApplicationTag), _application))
                {
                    _activities.Enqueue(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    public void Dispose()
    {
        _meterListener.Dispose();
        _activityListener.Dispose();
        Directory.Delete(_promptDirectory, recursive: true);
    }

    private void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var tagMap = new Dictionary<string, object?>();
        foreach (var tag in tags)
        {
            tagMap[tag.Key] = tag.Value;
        }

        if (Equals(tagMap.GetValueOrDefault(AgentTelemetry.ApplicationTag), _application))
        {
            _measurements.Enqueue(new Measurement(instrument.Name, value, tagMap));
        }
    }

    private IReadOnlyList<Measurement> MeasurementsOf(string instrument) => [.. _measurements.Where(m => m.Instrument == instrument)];

    private void WriteSystemPrompt(string promptType, string content)
    {
        var path = Path.Combine(_promptDirectory, $"{promptType}.md");
        File.WriteAllText(path, content);
        _configuration[$"AiAgents:PromptFiles:SystemPrompts:{promptType}"] = path;
    }

    private ServiceProvider Build()
    {
        _configuration["AiAgents:ApplicationName"] = _application;
        _configuration["AiAgents:Foundry:Endpoint"] = "https://example.services.ai.azure.com/api/projects/test";
        _configuration["AiAgents:Foundry:DefaultModel"] = "gpt-4o";

        var services = new ServiceCollection().AddLogging();
        services.AddAiAgents(new ConfigurationBuilder().AddInMemoryCollection(_configuration).Build(),
            agents => agents.UseCredential(Substitute.For<TokenCredential>()));
        services.AddSingleton<AgentAdministrationClient>(_foundry.Admin);
        services.AddSingleton<IFoundryConversationClient>(_conversations);
        return services.BuildServiceProvider();
    }

    private static Task<string> PromptFor(AgentDefinition definition, CancellationToken _) => Task.FromResult($"Brief on {definition.Name}.");

    [Fact]
    public async Task EveryRun_RecordsTokensAndDuration_TaggedWithTheApplicationAndAgent()
    {
        WriteSystemPrompt("Ofsted", "You analyse Ofsted reports.");
        _conversations.Reply("ofsted-agent", FoundryResponses.Completed("r1", "Good.", totalTokens: 250));
        using var provider = Build();

        await provider.GetRequiredService<IAgentService>()
            .RunParallelAsync([new AgentDefinition("ofsted-agent", "Ofsted")], PromptFor, new AgentContext());

        var tokens = MeasurementsOf("aiagents.tokens");
        Assert.Equal(10, tokens.Single(m => Equals(m.Tags[AgentTelemetry.TokenTypeTag], "input")).Value);
        Assert.Equal(240, tokens.Single(m => Equals(m.Tags[AgentTelemetry.TokenTypeTag], "output")).Value);
        Assert.All(tokens, m => Assert.Equal("ofsted-agent", m.Tags[AgentTelemetry.AgentNameTag]));

        var duration = Assert.Single(MeasurementsOf("aiagents.run.duration"));
        Assert.Equal("success", duration.Tags[AgentTelemetry.OutcomeTag]);
        Assert.Equal("ofsted-agent", duration.Tags[AgentTelemetry.AgentNameTag]);
    }

    [Fact]
    public async Task EphemeralRuns_AreTaggedWithTheBaseAgentName_NotThePerRunName()
    {
        WriteSystemPrompt("WebSearch", "You search the web.");
        _conversations.Reply("web-search-agent", FoundryResponses.Completed("r1", "News."));
        using var provider = Build();
        var runner = provider.GetRequiredService<IAgentService>();
        var definition = new AgentDefinition("web-search-agent", "WebSearch", IsManagedAgent: false);

        await runner.RunParallelAsync([definition], PromptFor, new AgentContext());
        await runner.RunParallelAsync([definition], PromptFor, new AgentContext());

        Assert.All(MeasurementsOf("aiagents.run.duration"), m => Assert.Equal("web-search-agent", m.Tags[AgentTelemetry.AgentNameTag]));
        Assert.All(_activities.Where(a => a.OperationName == "invoke_agent"),
            a => Assert.Equal("web-search-agent", a.GetTagItem(AgentTelemetry.AgentNameTag)));
    }

    [Fact]
    public async Task AnOrchestration_RecordsItsTotalTokens_AndParentsEachAgentRunSpan()
    {
        WriteSystemPrompt("Ofsted", "You analyse Ofsted reports.");
        WriteSystemPrompt("Trust", "You analyse trusts.");
        _conversations.Reply("ofsted-agent", FoundryResponses.Completed("r1", "Good.", totalTokens: 100));
        _conversations.Reply("trust-agent", FoundryResponses.Completed("r2", "Stable.", totalTokens: 60));
        using var provider = Build();

        await provider.GetRequiredService<IAgentService>().RunParallelAsync(
            [new AgentDefinition("ofsted-agent", "Ofsted"), new AgentDefinition("trust-agent", "Trust")], PromptFor, new AgentContext());

        var total = Assert.Single(MeasurementsOf("aiagents.orchestration.tokens"));
        Assert.Equal(160, total.Value);
        Assert.Equal("parallel", total.Tags[AgentTelemetry.ModeTag]);

        var orchestration = Assert.Single(_activities, a => a.OperationName == "orchestrate_agents");
        Assert.Equal(20L, orchestration.GetTagItem("gen_ai.usage.input_tokens"));
        Assert.Equal(140L, orchestration.GetTagItem("gen_ai.usage.output_tokens"));
        var runs = _activities.Where(a => a.OperationName == "invoke_agent").ToList();
        Assert.Equal(2, runs.Count);
        Assert.All(runs, run => Assert.Equal(orchestration.SpanId, run.ParentSpanId));
    }

    [Fact]
    public async Task ABriefingsTokenUsage_AddsUpSpecialistsAndSynthesis_InTotalAndPerAgent()
    {
        WriteSystemPrompt("Ofsted", "You analyse Ofsted reports.");
        WriteSystemPrompt("Trust", "You analyse trusts.");
        _conversations.Reply("ofsted-agent", FoundryResponses.Completed("r1", "Good.", totalTokens: 100));
        _conversations.Fail("trust-agent", new InvalidOperationException("Model error."));
        _conversations.Reply("briefing-synthesis-agent", FoundryResponses.Completed("r3", "Briefing.", totalTokens: 300));
        using var provider = Build();

        var specialists = await provider.GetRequiredService<IAgentService>().RunParallelAsync(
            [new AgentDefinition("ofsted-agent", "Ofsted"), new AgentDefinition("trust-agent", "Trust")], PromptFor, new AgentContext());
        var synthesis = await provider.GetRequiredService<IAgentRuntime>().RunEphemeralAsync(
            new AgentSpec { Name = "briefing-synthesis-agent", Instructions = "Write the briefing." }, "Combine the findings.");

        var usage = specialists.Append(synthesis).ToTokenUsageSummary();

        Assert.Equal(new TokenUsage(20, 380, 400), usage.Total);
        Assert.Equal(new TokenUsage(10, 90, 100), usage.ByAgent["ofsted-agent"]);
        Assert.Equal(TokenUsage.None, usage.ByAgent["trust-agent"]);
        Assert.Equal(new TokenUsage(10, 290, 300), usage.ByAgent["briefing-synthesis-agent"]);
    }

    [Fact]
    public async Task TheLowerLevelOrchestrator_AlsoRecordsItsTotalTokens_IncludingFailedSteps()
    {
        _conversations.Reply("ofsted-agent", FoundryResponses.Completed("r1", "Good.", totalTokens: 100));
        _conversations.Reply("trust-agent", FoundryResponses.FunctionCall("r2", "call-1", "lookup")); // no callback: fails after one round
        using var provider = Build();
        var factory = provider.GetRequiredService<GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces.IAgentFactory>();
        var ofsted = await factory.GetOrCreateAsync(new AgentSpec { Name = "ofsted-agent", Instructions = "x" });
        var trust = await factory.GetOrCreateAsync(new AgentSpec { Name = "trust-agent", Instructions = "y" });

        var result = await provider.GetRequiredService<IAgentRuntime>().Orchestrator
            .RunParallelAsync([ofsted, trust], "input", new AgentContext());

        Assert.False(result.Results.Single(r => r.AgentName == "trust-agent").Succeeded);
        var total = Assert.Single(MeasurementsOf("aiagents.orchestration.tokens"));
        Assert.Equal(result.Usage.TotalTokens, total.Value);
        Assert.Single(_activities, a => a.OperationName == "orchestrate_agents");
    }

    [Fact]
    public void ApplicationName_DefaultsToTheEntryAssemblyName()
        => Assert.Equal(System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown", new AgentRunOptions().ApplicationName);
}
