using Azure.AI.Extensions.OpenAI;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Quality;
using GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration.Fakes;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Quality;

/// <summary>The four issues found building a release gate: judge client, lost reasons, reasoning models, publishing.</summary>
public sealed class QualityIssueFixTests
{
    private static readonly AgentRunSample Sample = new("ofsted-agent", "3", "gpt-4o", "Summarise.", "Rated Good.", "Rated Good.");

    // ===================== 1 and 3: the built-in judge =====================

    private static (FoundryJudgeChatClient Judge, Func<CreateResponseOptions?> Sent) Judge(ResponseResult reply)
    {
        CreateResponseOptions? sent = null;
        var responses = Substitute.For<ProjectResponsesClient>();
        responses.CreateResponseAsync(Arg.Do<CreateResponseOptions>(options => sent = options), Arg.Any<CancellationToken>())
            .Returns(ClientResult.FromValue(reply, Substitute.For<PipelineResponse>()));
        var project = Substitute.For<ProjectOpenAIClient>();
        project.GetProjectResponsesClient().Returns(responses);
        return (new FoundryJudgeChatClient(project, "myconnection/gpt-5.1"), () => sent);
    }

    [Fact]
    public async Task Judge_CallsTheModelThroughFoundry_AndReturnsItsAnswerAndUsage()
    {
        var (judge, sent) = Judge(FoundryResponses.Completed("r1", "{\"score\": 4}", totalTokens: 50));

        var response = await judge.GetResponseAsync([new(ChatRole.System, "You grade answers."), new(ChatRole.User, "Grade this.")]);

        Assert.Equal("{\"score\": 4}", response.Text);
        Assert.Equal(50, response.Usage!.TotalTokenCount);
        Assert.Equal("myconnection/gpt-5.1", sent()!.Model);
        Assert.Equal(2, sent()!.InputItems.Count);
    }

    [Fact]
    public async Task Judge_SendsOnlyTheModelAndMessages_SoReasoningModelsAcceptIt()
    {
        var (judge, sent) = Judge(FoundryResponses.Completed("r1", "{\"score\": 4}"));

        await judge.GetResponseAsync([new(ChatRole.User, "Grade this.")],
            new ChatOptions { Temperature = 0, TopP = 1, MaxOutputTokens = 800, FrequencyPenalty = 0, PresencePenalty = 0 });

        Assert.Null(sent()!.Temperature);
        Assert.Null(sent()!.TopP);
        Assert.Null(sent()!.MaxOutputTokenCount);
    }

    // ===================== 2: why a score is missing =====================

    [Fact]
    public async Task AMissingScore_IsLoggedWithItsReason_WithoutTheStackTrace()
    {
        var metric = new NumericMetric("Groundedness");
        metric.AddDiagnostics(EvaluationDiagnostic.Error("Judge call failed: 400 Unsupported parameter 'temperature'.\n   at Some.Stack.Frame()"));
        var evaluator = Substitute.For<IEvaluator>();
        evaluator.EvaluateAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatResponse>(), Arg.Any<ChatConfiguration?>(),
                Arg.Any<IEnumerable<EvaluationContext>?>(), Arg.Any<CancellationToken>())
            .Returns(new EvaluationResult(metric, new NumericMetric("Relevance", 5)));
        var logs = new CollectingLoggerProvider();
        using var loggers = LoggerFactory.Create(builder => builder.AddProvider(logs));

        var scores = await new ExtensionsAiEvaluator(evaluator, new ChatConfiguration(Substitute.For<IChatClient>()),
            loggers.CreateLogger<ExtensionsAiEvaluator>()).EvaluateAsync(Sample);

        Assert.Equal(new Dictionary<string, double> { ["Relevance"] = 5 }, scores);
        var warning = Assert.Single(logs.AtLevel(LogLevel.Warning));
        Assert.Contains("No Groundedness score for ofsted-agent: Judge call failed: 400 Unsupported parameter 'temperature'.",
            warning.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Some.Stack.Frame", warning.Message, StringComparison.Ordinal);
    }

    // ===================== 4: testing without publishing =====================

    private static readonly AgentDefinition Ofsted = new("ofsted-agent", "Ofsted");
    private static readonly AgentTestCase Case = new("good-school", "Summarise.") { MustMention = ["Good"] };

    private static IAgentService Answering()
    {
        var agents = Substitute.For<IAgentService>();
        agents.RunAsync(Arg.Any<AgentDefinition>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentResult("ofsted-agent", "Rated Good.", 30));
        return agents;
    }

    [Fact]
    public async Task TestRunner_ByDefault_TestsAnEphemeralCopy_SoNothingIsPublished()
    {
        var agents = Answering();

        var report = await new AgentTestRunner(agents).RunAsync(Ofsted, [Case]);

        Assert.True(report.Passed);
        await agents.Received(1).RunAsync(Arg.Is<AgentDefinition>(tested => tested.Name == "ofsted-agent" && !tested.IsManagedAgent),
            "Summarise.", Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TestRunner_CanTestTheDeployedVersion()
    {
        var agents = Answering();

        await new AgentTestRunner(agents).RunAsync(Ofsted, [Case], AgentTestTarget.Deployed);

        await agents.Received(1).RunAsync(Ofsted, "Summarise.", Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
