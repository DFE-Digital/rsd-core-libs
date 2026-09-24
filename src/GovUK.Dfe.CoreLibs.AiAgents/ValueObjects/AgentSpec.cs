using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>
/// Defines the configuration for a Foundry agent.
/// </summary>
public sealed record AgentSpec
{
    /// <summary>The stable name of the agent.</summary>
    public required string Name { get; init; }

    /// <summary>The system instructions for the agent.</summary>
    public required string Instructions { get; init; }

    /// <summary>
    /// The model deployment, or the default from <see cref="FoundryAgentFactoryOptions.DefaultModel"/>.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>An optional description of the agent.</summary>
    public string? Description { get; init; }

    /// <summary>The tools available to the agent.</summary>
    public IReadOnlyList<ResponseTool> Tools { get; init; } = [];
}
