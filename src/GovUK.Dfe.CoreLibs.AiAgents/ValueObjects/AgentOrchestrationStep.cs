namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>One step of an orchestration: which agent to run and what to send it.</summary>
public sealed record AgentOrchestrationStep(string AgentName, Func<CancellationToken, Task<AgentReference>> ResolveAgent,
    Func<CancellationToken, Task<string>> ResolvePrompt)
{
    /// <summary>Untrusted material for the agent (search results, documents), sent fenced as data. Null for none.</summary>
    public Func<CancellationToken, Task<string?>>? ResolveEvidence { get; init; }

    /// <summary>
    /// Runs the model's function tool calls in this app, e.g. from
    /// <c>AgentToolExecution.CreateResolver(providers, allowedTools)</c>. Null when the agent has none.
    /// </summary>
    public ToolCallResolver? ResolveToolCalls { get; init; }
}
