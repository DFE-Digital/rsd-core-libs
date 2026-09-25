namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>
/// Represents the result of an agent execution.
/// </summary>
/// <param name="AgentName">The agent name.</param>
/// <param name="Output">The agent's response, if any.</param>
/// <param name="TotalTokens">The total input and output tokens used.</param>
public sealed record AgentResult(string AgentName, string? Output, long TotalTokens);
