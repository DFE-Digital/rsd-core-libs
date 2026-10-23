using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Quality;

/// <summary>Which version of an agent the test cases run against.</summary>
public enum AgentTestTarget
{
    /// <summary>
    /// An ephemeral copy of the current prompt, tools and schema, deleted afterwards, so a failed gate publishes nothing.
    /// Not for externally managed agents, which have no local prompt.
    /// </summary>
    Candidate,

    /// <summary>The version this app runs today: its pin, its externally managed version, or its latest.</summary>
    Deployed,
}

/// <summary>Runs test cases against an agent, for a release gate in CI.</summary>
public interface IAgentTestRunner
{
    /// <summary>Runs the cases one at a time, checks each answer's facts, and scores it if <c>AddQualityEvaluation</c> is set up.</summary>
    /// <param name="target">Candidate (default) tests without publishing; Deployed tests what's running now.</param>
    Task<AgentEvaluationReport> RunAsync(AgentDefinition definition, IReadOnlyCollection<AgentTestCase> cases,
        AgentTestTarget target = AgentTestTarget.Candidate, CancellationToken cancellationToken = default);
}

internal sealed class AgentTestRunner(IAgentService agents, IAgentRunEvaluator? evaluator = null) : IAgentTestRunner
{
    private static readonly IReadOnlyDictionary<string, double> NoScores = new Dictionary<string, double>();

    public async Task<AgentEvaluationReport> RunAsync(AgentDefinition definition, IReadOnlyCollection<AgentTestCase> cases,
        AgentTestTarget target = AgentTestTarget.Candidate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(cases);

        // An ephemeral copy runs the same prompt, tools, schema and checks, without creating a managed version.
        var tested = target == AgentTestTarget.Candidate ? definition with { IsManagedAgent = false } : definition;

        var results = new List<AgentTestResult>(cases.Count);
        foreach (var testCase in cases)
        {
            results.Add(await RunCaseAsync(tested, testCase, cancellationToken).ConfigureAwait(false));
        }

        return new AgentEvaluationReport(definition.Name, results);
    }

    private async Task<AgentTestResult> RunCaseAsync(AgentDefinition definition, AgentTestCase testCase, CancellationToken cancellationToken)
    {
        AgentResult result;
        try
        {
            result = await agents.RunAsync(definition, testCase.Prompt, testCase.Evidence, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AgentTestResult(testCase.Name, null, [$"The run failed: {ex.GetBaseException().Message}"], NoScores);
        }

        var output = result.Output ?? string.Empty;
        List<string> failures =
        [
            .. testCase.MustMention.Where(fact => !output.Contains(fact, StringComparison.OrdinalIgnoreCase)).Select(fact => $"Doesn't mention \"{fact}\""),
            .. testCase.MustNotMention.Where(fact => output.Contains(fact, StringComparison.OrdinalIgnoreCase)).Select(fact => $"Mentions \"{fact}\""),
        ];

        var scores = evaluator is null
            ? NoScores
            : await evaluator.EvaluateAsync(new AgentRunSample(definition.Name, result.AgentVersion, result.Model, testCase.Prompt,
                testCase.Evidence, output), cancellationToken).ConfigureAwait(false);

        return new AgentTestResult(testCase.Name, output, failures, scores);
    }
}
