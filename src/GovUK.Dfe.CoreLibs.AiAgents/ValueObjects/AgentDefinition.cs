namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>
/// Represents the definition of an agent, including its name, system prompt type, evidence category, search query template, and whether it is a managed agent.
/// </summary>
/// <param name="Name">The agent name.</param>
/// <param name="SystemPromptType">The system prompt type.</param>
/// <param name="EvidenceCategory">The evidence category for specialist agents.</param>
/// <param name="SearchQueryTemplate">The search query template for specialist agents.</param>
/// <param name="IsManagedAgent">Whether this agent persists across calls.</param>
public sealed record AgentDefinition(string Name, string SystemPromptType, string? EvidenceCategory = null,
    string? SearchQueryTemplate = null, bool IsManagedAgent = true);
