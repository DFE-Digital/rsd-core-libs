namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>The result of an orchestration.</summary>
public sealed record OrchestrationResult(string? FinalOutput, IReadOnlyList<AgentStepResult> Results)
{
    public long TotalTokens => Results.Sum(r => r.Result?.TotalTokens ?? 0);

    /// <summary>Tokens used by every step, including failed ones, since Foundry billed them.</summary>
    public TokenUsage Usage => Results.Aggregate(TokenUsage.None,
        (total, step) => total + (step.Result?.Usage ?? Diagnostics.AgentTelemetry.TokenUsageOf(step.Error)));
}

/// <summary>One step: its result, or the error if it failed.</summary>
public sealed record AgentStepResult(string AgentName, AgentResult? Result, Exception? Error)
{
    public bool Succeeded => Error is null;
}
