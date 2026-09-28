namespace GovUK.Dfe.CoreLibs.AiAgents;

/// <summary>Settings for every agent run, read from the <c>AiAgents</c> section by <c>AddAiAgents</c>.</summary>
public sealed record AgentRunOptions
{
    /// <summary>Tags every span and metric. Defaults to the entry assembly's name.</summary>
    public string ApplicationName { get; init; } = Diagnostics.AgentTelemetry.DefaultApplicationName;

    /// <summary>Longest one run may take, tool rounds included; then it fails with <see cref="TimeoutException"/>. Null: no limit.</summary>
    public TimeSpan? RunTimeout { get; init; }

    /// <summary>Deletes each conversation the runner created, so prompts and evidence aren't kept in Foundry.</summary>
    public bool DeleteConversationsAfterRun { get; init; } = true;

    /// <summary>Runs at once on this instance, across every caller. Null: no limit.</summary>
    public int? MaxConcurrency { get; init; }

    /// <summary>Characters of one tool output sent to the model; the rest is cut with a note.</summary>
    public int MaxToolOutputCharacters { get; init; } = 20_000;

    /// <summary>Fences tool output as data the model mustn't take instructions from, like evidence.</summary>
    public bool FenceToolOutput { get; init; } = true;

    /// <summary>Characters of evidence per run; the rest is cut with a note, keeping the start.</summary>
    public int MaxEvidenceCharacters { get; init; } = 100_000;

    /// <summary>Longest a run waits for a slot before failing with <see cref="TimeoutException"/>.</summary>
    public TimeSpan MaxWaitForRunSlot { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>Fails startup when nothing records token metrics. Turn off only for local development and tests.</summary>
    public bool RequireTokenUsageTelemetry { get; init; } = true;

    /// <summary>At startup, checks this app can run every tool its pinned and externally managed agents call.</summary>
    public bool ValidateAgentToolsAtStartup { get; init; } = true;
}
