namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>
/// Represents the result of an agent execution.
/// </summary>
/// <param name="AgentName">The agent name.</param>
/// <param name="Output">The agent's response, if any.</param>
/// <param name="TotalTokens">The total input and output tokens used.</param>
public sealed record AgentResult(string AgentName, string? Output, long TotalTokens)
{
    /// <summary>The input (prompt) tokens used, when Foundry reported them.</summary>
    public long InputTokens { get; init; }

    /// <summary>The output (completion) tokens used, when Foundry reported them.</summary>
    public long OutputTokens { get; init; }

    /// <summary>This run's token usage.</summary>
    public TokenUsage Usage => new(InputTokens, OutputTokens, TotalTokens);
}
