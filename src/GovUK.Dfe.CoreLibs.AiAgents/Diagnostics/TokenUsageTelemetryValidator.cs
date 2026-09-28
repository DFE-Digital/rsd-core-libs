using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using Microsoft.Extensions.Hosting;

namespace GovUK.Dfe.CoreLibs.AiAgents.Diagnostics;

/// <summary>
/// Fails application startup when nothing is subscribed to the library's token usage metrics, so no app
/// can run agents without its token usage being measured. Registered by every <c>AddFoundryAgents</c>
/// overload (and so by <c>AddAgentExecution</c>); skipped when
/// <see cref="AgentRunOptions.RequireTokenUsageTelemetry"/> is <see langword="false"/>.
/// </summary>
/// <remarks>
/// The check confirms something is subscribed, not that data reaches its destination - a wrong
/// connection string still passes. It runs in <see cref="StartedAsync"/>, after every hosted service
/// has started, because OpenTelemetry attaches its metric listener while the host starts.
/// </remarks>
public sealed class TokenUsageTelemetryValidator : IHostedLifecycleService
{
    private readonly bool _required;
    private readonly Func<bool> _isTokenUsageRecorded;

    public TokenUsageTelemetryValidator(AgentRunOptions runOptions)
        : this(runOptions.RequireTokenUsageTelemetry, () => AgentTelemetry.IsTokenUsageRecorded)
    {
    }

    internal TokenUsageTelemetryValidator(bool required, Func<bool> isTokenUsageRecorded)
    {
        _required = required;
        _isTokenUsageRecorded = isTokenUsageRecorded;
    }

    public Task StartedAsync(CancellationToken cancellationToken)
        => !_required || _isTokenUsageRecorded()
            ? Task.CompletedTask
            : throw new InvalidOperationException(ErrorMessages.TokenUsageTelemetryNotConfigured);

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
