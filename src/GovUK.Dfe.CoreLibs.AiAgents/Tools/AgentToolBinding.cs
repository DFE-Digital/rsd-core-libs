namespace GovUK.Dfe.CoreLibs.AiAgents.Tools;

/// <summary>
/// Represents a binding between an agent and its tool provider.
/// </summary>
/// <param name="AgentName">The name of the agent.</param>
/// <param name="Provider">The tool provider.</param>
public sealed record AgentToolBinding(string AgentName, IAgentToolProvider Provider);
