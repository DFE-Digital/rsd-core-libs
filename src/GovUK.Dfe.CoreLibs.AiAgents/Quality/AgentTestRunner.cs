using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Quality;

/// <summary>Runs test cases against an agent, for a release gate in CI.</summary>
public interface IAgentTestRunner
{
    /// <summary>
    /// Runs each case, one at a time, through the version this app would run: in a provisioning job, the new one.
    /// Checks each answer's facts, and scores it when <c>AddQualityEvaluation</c> is set up.
    /// </summary>
    Task<AgentEvaluationReport> RunAsync(AgentDefinition definition, IReadOnlyCollection<AgentTestCase> cases,
        CancellationToken cancellationToken = default);
}

internal sealed class AgentTestRunner(IAgentService agents, IAgentRunEvaluator? evaluator = null) : IAgentTestRunner
{
    private static readonly IReadOnlyDictionary<string, double> NoScores = new Dictionary<string, double>();

    public async Task<AgentEvaluationReport> RunAsync(AgentDefinition definition, IReadOnlyCollection<AgentTestCase> cases,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(cases);

        var results = new List<AgentTestResult>(cases.Count);
        foreach (var testCase in cases)
        {
            results.Add(await RunCaseAsync(definition, testCase, cancellationToken).ConfigureAwait(false));
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
