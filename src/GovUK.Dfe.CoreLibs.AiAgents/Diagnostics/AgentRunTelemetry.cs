using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;
using System.Diagnostics;

namespace GovUK.Dfe.CoreLibs.AiAgents.Diagnostics;

/// <summary>The span and metrics for one agent run: start it, set the outcome, then <see cref="Record"/> once.</summary>
internal sealed class AgentRunTelemetry : IDisposable
{
    private readonly Activity? _activity;
    private readonly KeyValuePair<string, object?> _application;
    private readonly KeyValuePair<string, object?> _agent;
    private readonly long _started = Stopwatch.GetTimestamp();
    private string _outcome = "failure";
    private string? _model;

    public AgentRunTelemetry(string applicationName, AgentReference agent)
    {
        // Ephemeral agents are reported under their base name, so a run's metrics group with its agent.
        var agentName = AgentTelemetry.AgentNameForTelemetry(agent.Name);
        _application = new(AgentTelemetry.ApplicationTag, applicationName);
        _agent = new(AgentTelemetry.AgentNameTag, agentName);

        _activity = AgentTelemetry.ActivitySource.StartActivity("invoke_agent", ActivityKind.Client);
        _activity?.SetTag(AgentTelemetry.ApplicationTag, applicationName);
        _activity?.SetTag(AgentTelemetry.AgentNameTag, agentName);
        _activity?.SetTag(AgentTelemetry.AgentVersionTag, agent.Version);
    }

    /// <summary>The model that answered, so quality and cost can be tied to model changes.</summary>
    public void AnsweredBy(string? model)
    {
        _model = model;
        _activity?.SetTag(AgentTelemetry.ResponseModelTag, model);
    }

    public void Succeeded() => _outcome = "success";

    public void Cancelled() => _outcome = "cancelled";

    public void Failed(string outcome, string description)
    {
        _outcome = outcome;
        _activity?.SetStatus(ActivityStatusCode.Error, description);
    }

    /// <summary>Records tokens and duration, whatever the outcome: every response Foundry returned is billed.</summary>
    public void Record(TokenUsage usage)
    {
        KeyValuePair<string, object?> model = new(AgentTelemetry.ResponseModelTag, _model);
        AgentTelemetry.Tokens.Add(usage.InputTokens, _application, _agent, model, new(AgentTelemetry.TokenTypeTag, "input"));
        AgentTelemetry.Tokens.Add(usage.OutputTokens, _application, _agent, model, new(AgentTelemetry.TokenTypeTag, "output"));
        _activity?.SetTag("gen_ai.usage.input_tokens", usage.InputTokens);
        _activity?.SetTag("gen_ai.usage.output_tokens", usage.OutputTokens);
        AgentTelemetry.RunDuration.Record(Stopwatch.GetElapsedTime(_started).TotalSeconds,
            _application, _agent, model, new(AgentTelemetry.OutcomeTag, _outcome));
    }

    public void Dispose() => _activity?.Dispose();
}
