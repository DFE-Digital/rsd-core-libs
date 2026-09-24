namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>
/// Represents a reference to an agent, including its unique identifier, name, and optional version.
/// </summary>
/// <param name="Id">The unique identifier of this agent.</param>
/// <param name="Name">The agent name.</param>
/// <param name="Version">The specific version this reference points to, or null when it means "whatever Foundry currently considers latest".</param>
public sealed record AgentReference(string Id, string Name, string? Version = null);
