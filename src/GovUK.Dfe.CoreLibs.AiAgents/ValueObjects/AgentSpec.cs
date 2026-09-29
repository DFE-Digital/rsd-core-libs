using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using OpenAI.Responses;

namespace GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

/// <summary>What a Foundry agent version is built from.</summary>
public sealed record AgentSpec
{
    /// <summary>Its stable name.</summary>
    public required string Name { get; init; }

    /// <summary>Its instructions.</summary>
    public required string Instructions { get; init; }

    /// <summary>The model deployment, or the default from <see cref="FoundryAgentFactoryOptions.DefaultModel"/>.</summary>
    public string? Model { get; init; }

    /// <summary>Optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Its tools.</summary>
    public IReadOnlyList<ResponseTool> Tools { get; init; } = [];

    /// <summary>A JSON schema the agent's answer must follow, or <see langword="null"/> for free text.</summary>
    public AgentOutputSchema? OutputSchema { get; init; }
}
