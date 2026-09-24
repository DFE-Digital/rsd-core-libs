namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>
/// Represents a step in the orchestration of an agent, including the agent's name, a function to resolve the agent, and a function to resolve the prompt.
/// </summary>
/// <param name="AgentName">The name of the agent.</param>
/// <param name="ResolveAgent">A function to resolve the agent reference.</param>
/// <param name="ResolvePrompt">A function to resolve the prompt.</param>
public sealed record AgentOrchestrationStep(string AgentName, Func<CancellationToken, Task<AgentReference>> ResolveAgent,
    Func<CancellationToken, Task<string>> ResolvePrompt);
