using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration.Fakes;

/// <summary>Agent definitions for tests; stands in for what <c>AddAgents</c> registers.</summary>
internal sealed class FakeAgentDefinitions : IAgentDefinitionProvider
{
    public IReadOnlyCollection<AgentDefinition> Definitions { get; set; } = [];

    public IReadOnlyCollection<AgentDefinition> GetAgentsDefinitions() => Definitions;
}
