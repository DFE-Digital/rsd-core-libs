using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;

namespace GovUK.Dfe.CoreLibs.AiAgents.Quality;

/// <summary>
/// Scores answers with Microsoft.Extensions.AI.Evaluation evaluators, e.g. <see cref="GroundednessEvaluator"/> and
/// <see cref="RelevanceEvaluator"/>, judged by the model in <paramref name="chatConfiguration"/>. The run's evidence
/// is the grounding context.
/// </summary>
public sealed class ExtensionsAiEvaluator(IEvaluator evaluator, ChatConfiguration chatConfiguration) : IAgentRunEvaluator
{
    public async Task<IReadOnlyDictionary<string, double>> EvaluateAsync(AgentRunSample sample, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sample);

        ChatMessage[] messages = [new(ChatRole.User, sample.Prompt)];
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, sample.Output));
        EvaluationContext[] context = sample.Evidence is null ? [] : [new GroundednessEvaluatorContext(sample.Evidence)];

        var result = await evaluator.EvaluateAsync(messages, response, chatConfiguration, context, cancellationToken).ConfigureAwait(false);
        return result.Metrics.Values.OfType<NumericMetric>()
            .Where(static metric => metric.Value is not null)
            .ToDictionary(static metric => metric.Name, static metric => metric.Value!.Value);
    }
}
