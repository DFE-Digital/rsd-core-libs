namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>
/// Represents the result of an orchestration.
/// </summary>
/// <param name="FinalOutput">The combined final output.</param>
/// <param name="Results">The results of each agent step.</param>
public sealed record OrchestrationResult(string? FinalOutput, IReadOnlyList<AgentStepResult> Results)
{
    public long TotalTokens => Results.Sum(r => r.Result?.TotalTokens ?? 0);
}

/// <summary>
/// Represents the result of a single agent step.
/// </summary>
/// <param name="AgentName">The agent that ran.</param>
/// <param name="Result">The agent result, if successful.</param>
/// <param name="Error">The error, if the step failed.</param>
public sealed record AgentStepResult(string AgentName, AgentResult? Result, Exception? Error)
{
    public bool Succeeded => Error is null;
}
