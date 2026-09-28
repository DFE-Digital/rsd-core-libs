namespace GovUK.Dfe.CoreLibs.AiAgents;

/// <summary>
/// Settings that apply to every agent run. Registered by <c>AddAgentExecution</c> (from
/// <see cref="AgentExecutionOptions"/>) or with defaults by <c>AddFoundryAgents</c>.
/// </summary>
public sealed record AgentRunOptions
{
    /// <summary>
    /// The application running the agents, tagged on every span and metric as
    /// <see cref="Diagnostics.AgentTelemetry.ApplicationTag"/> so usage can be split per application.
    /// Defaults to the name of the app's entry assembly.
    /// </summary>
    public string ApplicationName { get; init; } = Diagnostics.AgentTelemetry.DefaultApplicationName;

    /// <summary>
    /// The longest a single agent run may take, including tool-call rounds. A run that takes longer
    /// fails with a <see cref="TimeoutException"/>, which orchestration isolates like any other failure.
    /// <see langword="null"/> (the default) means no limit beyond the caller's cancellation token.
    /// </summary>
    public TimeSpan? RunTimeout { get; init; }

    /// <summary>
    /// Whether to delete each conversation the runner created once its run finishes, so prompts and
    /// evidence aren't retained in Foundry. Conversations passed in by the caller are never deleted.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool DeleteConversationsAfterRun { get; init; } = true;

    /// <summary>
    /// The most agent runs this instance makes at once, across every caller. Extra runs wait for a slot.
    /// <see langword="null"/> (the default) means no limit. For a limit across instances, see <c>GlobalConcurrency</c>.
    /// </summary>
    public int? MaxConcurrency { get; init; }

    /// <summary>
    /// The most characters of one tool call's output sent back to the model. Longer output is cut off
    /// with a note saying how much was left out. Defaults to 20,000 (roughly 5,000 tokens).
    /// </summary>
    public int MaxToolOutputCharacters { get; init; } = 20_000;

    /// <summary>
    /// The most characters of evidence sent with one run. The rest is cut off with a note, keeping the
    /// start, which holds the most relevant results. Defaults to 100,000 (roughly 25,000 tokens).
    /// </summary>
    public int MaxEvidenceCharacters { get; init; } = 100_000;

    /// <summary>
    /// How long a run waits for a free slot (per-instance or global) before failing with a
    /// <see cref="TimeoutException"/>. Defaults to 2 minutes.
    /// </summary>
    public TimeSpan MaxWaitForRunSlot { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Whether startup must fail when nothing records the token usage metrics. Defaults to
    /// <see langword="true"/>; set <see langword="false"/> only for local development and tests.
    /// </summary>
    public bool RequireTokenUsageTelemetry { get; init; } = true;

    /// <summary>
    /// Whether startup checks that this app can run every tool its pinned and externally managed agents
    /// call. Defaults to <see langword="true"/>.
    /// </summary>
    public bool ValidateAgentToolsAtStartup { get; init; } = true;
}
