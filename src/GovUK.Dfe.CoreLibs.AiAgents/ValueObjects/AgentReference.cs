namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>A resolved agent.</summary>
/// <param name="Version">Null: the latest.</param>
public sealed record AgentReference(string Id, string Name, string? Version = null);
