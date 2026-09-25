using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;

/// <summary>
/// Provides agent definitions for the orchestrator, including the agents and synthesis agent.
/// </summary>
public interface IAgentDefinitionProvider
{
    /// <summary>
    /// Gets the agent definitions available to the orchestrator.
    /// </summary>
    /// <returns>The agent definitions.</returns>
    IReadOnlyCollection<AgentDefinition> GetAgentsDefinitions();
}
