using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GovUK.Dfe.CoreLibs.AiAgents.Quality;

/// <summary>
/// Scores answers with Microsoft.Extensions.AI.Evaluation evaluators, e.g. <see cref="GroundednessEvaluator"/> and
/// <see cref="RelevanceEvaluator"/>, judged by the model in <paramref name="chatConfiguration"/>. The run's evidence
/// is the grounding context. A metric left unscored, by a failed judge call or an unreadable reply, is logged with the reason.
/// </summary>
public sealed class ExtensionsAiEvaluator(IEvaluator evaluator, ChatConfiguration chatConfiguration,
    ILogger<ExtensionsAiEvaluator>? logger = null) : IAgentRunEvaluator
{
    private readonly ILogger _logger = logger ?? NullLogger<ExtensionsAiEvaluator>.Instance;

    public async Task<IReadOnlyDictionary<string, double>> EvaluateAsync(AgentRunSample sample, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sample);

        ChatMessage[] messages = [new(ChatRole.User, sample.Prompt)];
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, sample.Output));
        EvaluationContext[] context = sample.Evidence is null ? [] : [new GroundednessEvaluatorContext(sample.Evidence)];

        var result = await evaluator.EvaluateAsync(messages, response, chatConfiguration, context, cancellationToken).ConfigureAwait(false);

        var scores = new Dictionary<string, double>();
        // Not only NumericMetric: when the judge call throws, CompositeEvaluator reports a plain EvaluationMetric.
        foreach (var metric in result.Metrics.Values)
        {
            if (metric is NumericMetric { Value: { } score })
            {
                scores[metric.Name] = score;
                continue;
            }

            _logger.LogWarning("No {Metric} score for {AgentName}: {Reason}", metric.Name, sample.AgentName, WhyUnscored(metric));
        }

        return scores;
    }

    /// <summary>The evaluator's diagnostics without stack traces, e.g. the judge call's error or an unreadable reply.</summary>
    internal static string WhyUnscored(EvaluationMetric metric)
    {
        var reasons = (metric.Diagnostics ?? [])
            .Select(static diagnostic => diagnostic.Message.Split("\n   at ", 2)[0].Trim())
            .Where(static reason => reason.Length > 0)
            .ToList();

        return reasons.Count > 0 ? string.Join(" | ", reasons) : metric.Reason ?? "the evaluator gave no reason";
    }
}
