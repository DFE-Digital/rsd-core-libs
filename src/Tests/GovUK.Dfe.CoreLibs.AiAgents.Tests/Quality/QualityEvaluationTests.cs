using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Diagnostics;
using GovUK.Dfe.CoreLibs.AiAgents.Quality;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Quality;

/// <summary>Live-run scoring, the Microsoft.Extensions.AI.Evaluation adapter, and the test-case release gate.</summary>
public sealed class QualityEvaluationTests
{
    private static readonly AgentDefinition Ofsted = new("ofsted-agent", "Ofsted");
    private static readonly AgentRunSample Sample = new("ofsted-agent", "3", "gpt-4o", "Summarise.", "Rated Good.", "Rated Good.");

    private static IAgentRunEvaluator ScoresGroundedness(double score)
    {
        var evaluator = Substitute.For<IAgentRunEvaluator>();
        evaluator.EvaluateAsync(Arg.Any<AgentRunSample>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, double> { ["Groundedness"] = score });
        return evaluator;
    }

    // ===================== Live runs =====================

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(1.0, true)]
    public void Sampling_FollowsTheRate(double rate, bool sampled)
        => Assert.Equal(sampled, new AgentQualityMonitor(ScoresGroundedness(5), rate, new AgentRunOptions(),
            NullLogger<AgentQualityMonitor>.Instance).ShouldSample());

    [Fact]
    public async Task EachScore_IsRecorded_TaggedWithTheAgentVersionAndMetric()
    {
        var recorded = new List<(double Score, Dictionary<string, object?> Tags)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == "aiagents.quality.score")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((_, score, tags, _) => recorded.Add((score, tags.ToArray().ToDictionary(t => t.Key, t => t.Value))));
        listener.Start();

        await new AgentQualityMonitor(ScoresGroundedness(4.5), 1, new AgentRunOptions { ApplicationName = "briefing-tool" },
            NullLogger<AgentQualityMonitor>.Instance).ScoreAsync(Sample, CancellationToken.None);

        var (score, tags) = Assert.Single(recorded, r => Equals(r.Tags[AgentTelemetry.ApplicationTag], "briefing-tool"));
        Assert.Equal(4.5, score);
        Assert.Equal("3", tags["gen_ai.agent.version"]);
        Assert.Equal("Groundedness", tags["aiagents.quality.metric"]);
    }

    [Fact]
    public async Task AScoringFailure_IsOnlyLogged()
    {
        var evaluator = Substitute.For<IAgentRunEvaluator>();
        evaluator.EvaluateAsync(Arg.Any<AgentRunSample>(), Arg.Any<CancellationToken>()).Throws(new InvalidOperationException("Judge down."));
        var monitor = new AgentQualityMonitor(evaluator, 1, new AgentRunOptions(), NullLogger<AgentQualityMonitor>.Instance);

        Assert.Null(await Record.ExceptionAsync(() => monitor.ScoreAsync(Sample, CancellationToken.None)));
    }

    // ===================== Microsoft.Extensions.AI.Evaluation =====================

    [Fact]
    public async Task ExtensionsAiEvaluator_PassesTheRunAsAChat_WithTheEvidenceAsGroundingContext_AndReturnsNumericScores()
    {
        var evaluator = Substitute.For<IEvaluator>();
        IEnumerable<EvaluationContext>? context = null;
        evaluator.EvaluateAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatResponse>(), Arg.Any<ChatConfiguration?>(),
                Arg.Do<IEnumerable<EvaluationContext>?>(c => context = c), Arg.Any<CancellationToken>())
            .Returns(new EvaluationResult(new NumericMetric("Groundedness", 4), new NumericMetric("Relevance", 5), new BooleanMetric("Unscored")));

        var scores = await new ExtensionsAiEvaluator(evaluator, new ChatConfiguration(Substitute.For<IChatClient>())).EvaluateAsync(Sample);

        Assert.Equal(new Dictionary<string, double> { ["Groundedness"] = 4, ["Relevance"] = 5 }, scores);
        Assert.IsType<GroundednessEvaluatorContext>(Assert.Single(context!));
    }

    // ===================== Release gate =====================

    private static IAgentService Answers(string output)
    {
        var agents = Substitute.For<IAgentService>();
        agents.RunAsync(Arg.Any<AgentDefinition>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentResult("ofsted-agent", output, 30) { AgentVersion = "4" });
        return agents;
    }

    private static readonly AgentTestCase GoodSchool = new("good-school", "Summarise URN 100000.")
    {
        MustMention = ["good", "2024"],
        MustNotMention = ["Outstanding"],
    };

    [Fact]
    public async Task TestRunner_ChecksEachCasesFacts_CaseInsensitively_AndScoresIt()
    {
        var report = await new AgentTestRunner(Answers("Rated GOOD in 2024."), ScoresGroundedness(4)).RunAsync(Ofsted, [GoodSchool]);

        Assert.True(report.Passed);
        Assert.Equal(4, report.AverageScores["Groundedness"]);
    }

    [Fact]
    public async Task TestRunner_ReportsEveryMissingOrForbiddenFact()
    {
        var report = await new AgentTestRunner(Answers("Rated Outstanding.")).RunAsync(Ofsted, [GoodSchool]);

        var result = Assert.Single(report.Results);
        Assert.False(report.Passed);
        Assert.Equal(["Doesn't mention \"good\"", "Doesn't mention \"2024\"", "Mentions \"Outstanding\""], result.Failures);
    }

    [Fact]
    public async Task TestRunner_RecordsAFailedRun_AsAFailedCase_AndCarriesOn()
    {
        var agents = Substitute.For<IAgentService>();
        agents.RunAsync(Arg.Any<AgentDefinition>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("outer", new TimeoutException("Took too long.")));

        var report = await new AgentTestRunner(agents).RunAsync(Ofsted, [GoodSchool, GoodSchool with { Name = "second" }]);

        Assert.Equal(2, report.Results.Count);
        Assert.All(report.Results, result => Assert.Equal(["The run failed: Took too long."], result.Failures));
    }

    [Fact]
    public void Report_FlagsMetricsBelowTheMinimum_AndRegressionsFromTheBaseline()
    {
        static AgentEvaluationReport Scored(double groundedness, double relevance) => new("ofsted-agent",
            [new AgentTestResult("c", "x", [], new Dictionary<string, double> { ["Groundedness"] = groundedness, ["Relevance"] = relevance })]);

        var baseline = Scored(4.5, 4.0);
        var candidate = Scored(3.9, 4.0);

        Assert.Equal(["Groundedness"], candidate.BelowMinimum(4));
        Assert.Equal(["Groundedness"], candidate.RegressionsFrom(baseline, tolerance: 0.2));
        Assert.Empty(Scored(4.4, 4.0).RegressionsFrom(baseline, tolerance: 0.2));
    }

    [Fact]
    public void Report_RoundTripsThroughJson_SoItCanBeTheNextBaseline()
    {
        var report = new AgentEvaluationReport("ofsted-agent",
            [new AgentTestResult("c", "x", [], new Dictionary<string, double> { ["Groundedness"] = 4.5 })]);

        var baseline = JsonSerializer.Deserialize<AgentEvaluationReport>(JsonSerializer.Serialize(report))!;

        Assert.Equal(4.5, baseline.AverageScores["Groundedness"]);
    }

    [Fact]
    public async Task TestCases_LoadFromJsonFiles_NamedAfterTheFile()
    {
        var directory = Directory.CreateTempSubdirectory("aiagents-cases-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "urn-100000.json"),
                """{ "prompt": "Summarise URN 100000.", "evidence": "Rated Good.", "mustMention": [ "Good" ] }""");

            var testCase = Assert.Single(await AgentTestCase.LoadAsync(directory));

            Assert.Equal(("urn-100000", "Summarise URN 100000.", "Rated Good."), (testCase.Name, testCase.Prompt, testCase.Evidence));
            Assert.Equal(["Good"], testCase.MustMention);
            Assert.Empty(testCase.MustNotMention);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
