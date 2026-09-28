using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace GovUK.Dfe.CoreLibs.AiAgents.Diagnostics;

/// <summary>
/// The library's trace and metric sources. Nothing is sent anywhere until the application subscribes,
/// e.g. with OpenTelemetry: <c>.WithTracing(t =&gt; t.AddSource(AgentTelemetry.SourceName))</c> and
/// <c>.WithMetrics(m =&gt; m.AddMeter(AgentTelemetry.SourceName))</c>.
/// </summary>
/// <remarks>
/// The source name is the same in every application, so each one subscribes the same way. Every span
/// and measurement is tagged with <see cref="ApplicationTag"/> (set with
/// <c>AgentExecutionOptions.ApplicationName</c>) so usage can be split by the application that ran it.
/// </remarks>
public static class AgentTelemetry
{
    /// <summary>The name of both the <see cref="ActivitySource"/> and the <see cref="Meter"/>.</summary>
    public const string SourceName = "GovUK.Dfe.CoreLibs.AiAgents";

    /// <summary>The tag naming the application that ran the agent, on every span and measurement.</summary>
    public const string ApplicationTag = "aiagents.application";

    internal const string AgentNameTag = "gen_ai.agent.name";
    internal const string AgentVersionTag = "gen_ai.agent.version";
    internal const string TokenTypeTag = "gen_ai.token.type";
    internal const string OutcomeTag = "outcome";
    internal const string ModeTag = "aiagents.orchestration.mode";

    internal static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    /// <summary>Tokens used by each agent run, tagged with the application, agent and token type (input / output).</summary>
    internal static readonly Counter<long> Tokens = Meter.CreateCounter<long>(
        "aiagents.tokens", "{token}", "Tokens used by agent runs.");

    /// <summary>Duration of each agent run in seconds, tagged with the application, agent and outcome.</summary>
    internal static readonly Histogram<double> RunDuration = Meter.CreateHistogram<double>(
        "aiagents.run.duration", "s", "Duration of agent runs.");

    /// <summary>Total tokens used by one orchestration (e.g. a whole briefing), tagged with the application and mode.</summary>
    internal static readonly Histogram<double> RunSlotWait = Meter.CreateHistogram<double>(
        "aiagents.run.slot_wait", "s", "Time agent runs waited for a free run slot.");

    internal static readonly Histogram<long> OrchestrationTokens = Meter.CreateHistogram<long>(
        "aiagents.orchestration.tokens", "{token}", "Total tokens used by one orchestration of several agents.");

    /// <summary>
    /// Whether anything - normally the app's OpenTelemetry <c>MeterProvider</c> - is listening to the
    /// token usage metric. <see langword="false"/> means token usage would be measured and then dropped.
    /// </summary>
    public static bool IsTokenUsageRecorded => Tokens.Enabled;

    /// <summary>Used when no application name is configured: the name of the app's entry assembly.</summary>
    internal static string DefaultApplicationName => Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown";

    /// <summary>The <see cref="Exception.Data"/> key a failed run stores its token usage under.</summary>
    public const string TokenUsageDataKey = "aiagents.token_usage";

    /// <summary>
    /// The tokens a failed run used before it failed, read from the exception it threw (or one it wraps).
    /// </summary>
    public static ValueObjects.TokenUsage TokenUsageOf(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current.Data[TokenUsageDataKey] is ValueObjects.TokenUsage usage)
            {
                return usage;
            }
        }

        return ValueObjects.TokenUsage.None;
    }

    internal const string ParallelMode = "parallel";
    internal const string SequentialMode = "sequential";

    /// <summary>Starts the span for one orchestration of several agents (e.g. one briefing).</summary>
    internal static Activity? StartOrchestration(string applicationName, string mode, int agentCount)
    {
        var activity = ActivitySource.StartActivity("orchestrate_agents");
        activity?.SetTag(ApplicationTag, applicationName);
        activity?.SetTag(ModeTag, mode);
        activity?.SetTag("aiagents.agent_count", agentCount);
        return activity;
    }

    /// <summary>
    /// Records what a whole orchestration used, so the cost of one briefing can be seen as a single
    /// number as well as per agent run.
    /// </summary>
    internal static void RecordOrchestration(Activity? activity, string applicationName, string mode, ValueObjects.TokenUsage usage,
        int failedAgents)
    {
        activity?.SetTag("gen_ai.usage.input_tokens", usage.InputTokens);
        activity?.SetTag("gen_ai.usage.output_tokens", usage.OutputTokens);
        activity?.SetTag("aiagents.failed_agent_count", failedAgents);

        OrchestrationTokens.Record(usage.TotalTokens,
            new KeyValuePair<string, object?>(ApplicationTag, applicationName),
            new KeyValuePair<string, object?>(ModeTag, mode));
    }

    /// <summary>
    /// The agent name to tag telemetry with. Ephemeral agents drop their per-run GUID suffix, so every
    /// run of "web-search-agent" is counted together rather than creating a new metric series each time.
    /// </summary>
    internal static string AgentNameForTelemetry(string agentName)
        => Agents.AgentRuntime.IsEphemeralName(agentName) ? agentName[..^33] : agentName;
}
