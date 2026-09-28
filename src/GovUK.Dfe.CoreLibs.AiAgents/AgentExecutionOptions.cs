namespace GovUK.Dfe.CoreLibs.AiAgents;

/// <summary>
/// Options for configuring agent execution behavior, including response formatting, version pinning, and retry policies.
/// </summary>
internal sealed record AgentExecutionOptions
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

    /// <summary>The longest a single agent run may take. See <see cref="AgentRunOptions.RunTimeout"/>.</summary>
    public TimeSpan? RunTimeout { get; init; }

    /// <summary>Whether to delete conversations the runner created. See <see cref="AgentRunOptions.DeleteConversationsAfterRun"/>.</summary>
    public bool DeleteConversationsAfterRun { get; init; } = true;

    /// <summary>The most specialist agents run at once. See <see cref="AgentRunOptions.MaxConcurrency"/>.</summary>
    public int? MaxConcurrency { get; init; }

    /// <summary>
    /// The application running the agents, e.g. "school-briefing-service". Tagged on every span and
    /// metric so usage and cost can be split per application. Defaults to the entry assembly's name.
    /// </summary>
    public string? ApplicationName { get; init; }

    /// <summary>
    /// Whether the app must record token usage metrics. When <see langword="true"/> (the default),
    /// startup fails unless something subscribes to <see cref="Diagnostics.AgentTelemetry.SourceName"/>'s
    /// meter, e.g. OpenTelemetry with <c>.WithMetrics(m =&gt; m.AddMeter(AgentTelemetry.SourceName))</c>.
    /// Set to <see langword="false"/> only for local development and tests.
    /// </summary>
    public bool RequireTokenUsageTelemetry { get; init; } = true;

    /// <summary>See <see cref="AgentRunOptions.MaxEvidenceCharacters"/>.</summary>
    public int MaxEvidenceCharacters { get; init; } = 100_000;

    /// <summary>See <see cref="AgentRunOptions.MaxWaitForRunSlot"/>.</summary>
    public TimeSpan MaxWaitForRunSlot { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>The most characters of one tool call's output sent to the model. See <see cref="AgentRunOptions.MaxToolOutputCharacters"/>.</summary>
    public int MaxToolOutputCharacters { get; init; } = 20_000;

    /// <summary>Whether startup checks this app can run its pinned and externally managed agents' tools. See <see cref="AgentRunOptions.ValidateAgentToolsAtStartup"/>.</summary>
    public bool ValidateAgentToolsAtStartup { get; init; } = true;

    internal AgentRunOptions ToRunOptions() => new()
    {
        RequireTokenUsageTelemetry = RequireTokenUsageTelemetry,
        MaxToolOutputCharacters = MaxToolOutputCharacters,
        MaxEvidenceCharacters = MaxEvidenceCharacters,
        MaxWaitForRunSlot = MaxWaitForRunSlot,
        ValidateAgentToolsAtStartup = ValidateAgentToolsAtStartup,
        ApplicationName = string.IsNullOrWhiteSpace(ApplicationName) ? Diagnostics.AgentTelemetry.DefaultApplicationName : ApplicationName,
        RunTimeout = RunTimeout,
        DeleteConversationsAfterRun = DeleteConversationsAfterRun,
        MaxConcurrency = MaxConcurrency,
    };
}
