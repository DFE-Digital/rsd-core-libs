namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>Tokens used by one or more agent runs.</summary>
/// <param name="InputTokens">Prompt tokens, including instructions, evidence and tool results sent to the model.</param>
/// <param name="OutputTokens">Tokens the model generated.</param>
/// <param name="TotalTokens">All tokens, as Foundry reported them.</param>
public sealed record TokenUsage(long InputTokens, long OutputTokens, long TotalTokens)
{
    public static TokenUsage None { get; } = new(0, 0, 0);

    public static TokenUsage operator +(TokenUsage left, TokenUsage right)
        => new(left.InputTokens + right.InputTokens, left.OutputTokens + right.OutputTokens, left.TotalTokens + right.TotalTokens);
}

/// <summary>
/// Tokens used by several runs (e.g. a briefing), in total and per agent. Build with <c>results.ToTokenUsageSummary()</c>.
/// </summary>
public sealed record TokenUsageSummary(TokenUsage Total, IReadOnlyDictionary<string, TokenUsage> ByAgent);
