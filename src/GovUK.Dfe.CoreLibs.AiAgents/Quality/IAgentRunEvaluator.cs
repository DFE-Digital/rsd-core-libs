namespace GovUK.Dfe.CoreLibs.AiAgents.Quality;

/// <summary>One finished run, for scoring.</summary>
/// <param name="AgentVersion">Null for an ephemeral agent.</param>
public sealed record AgentRunSample(string AgentName, string? AgentVersion, string? Model, string Prompt, string? Evidence, string Output);

/// <summary>Scores an answer, e.g. its groundedness. Use <see cref="ExtensionsAiEvaluator"/> or your own.</summary>
public interface IAgentRunEvaluator
{
    /// <returns>Scores by metric name; higher is better.</returns>
    Task<IReadOnlyDictionary<string, double>> EvaluateAsync(AgentRunSample sample, CancellationToken cancellationToken = default);
}
