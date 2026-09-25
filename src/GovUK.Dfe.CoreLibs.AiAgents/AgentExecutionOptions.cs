namespace GovUK.Dfe.CoreLibs.AiAgents;

/// <summary>
/// Options for configuring agent execution behavior, including response formatting, version pinning, and retry policies.
/// </summary>
public sealed record AgentExecutionOptions
{
    /// <summary>
    /// The key for the prompt type that defines the standard response format for agents. If set, this prompt type will be appended to every system prompt before sending it to the agent.
    /// </summary>
    public string? ResponseFormatKey { get; init; }

    /// <summary>System prompt types that should not have the response format appended.</summary>
    public IReadOnlySet<string>? ResponseFormatExemptPromptTypes { get; init; }

    /// <summary>Whether to bind <see cref="Factories.AgentVersionPinningOptions"/> from configuration. Defaults to <see langword="true"/>.</summary>
    public bool EnableVersionPinning { get; init; } = true;

    /// <summary>
    /// Whether to enable drift detection for agent prompts. When enabled, the system will check for changes in prompt files and alert if any drift is detected. Defaults to <see langword="false"/>.
    /// </summary>
    public bool EnableDriftDetection { get; init; }

    /// <summary>The maximum number of Foundry client retries.</summary>
    public int MaxRetries { get; init; } = 3;
}
