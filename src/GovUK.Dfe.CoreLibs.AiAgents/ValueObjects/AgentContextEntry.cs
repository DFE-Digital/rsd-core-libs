namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>
/// Represents an agent's context history including input and output.
/// </summary>
/// <param name="AgentName">The agent name.</param>
/// <param name="Input">The agent input.</param>
/// <param name="Output">The agent output.</param>
public sealed record AgentContextEntry(string AgentName, string Input, string? Output);
