using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

/// <summary>
/// Deletes this app's orphaned ephemeral agents every <c>EphemeralAgentSweepInterval</c>. Runs on every instance,
/// which is safe: a delete another instance already made succeeds. A failed sweep is logged and retried next time.
/// </summary>
internal sealed class EphemeralAgentSweepService(IAgentRuntime runtime, AgentRunOptions runOptions, TimeSpan interval,
    ILogger<EphemeralAgentSweepService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // A random start spreads instances out, so they don't all list the project at once.
            await Task.Delay(TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32((int)Math.Min(interval.TotalMilliseconds, int.MaxValue))),
                stoppingToken).ConfigureAwait(false);

            using var timer = new PeriodicTimer(interval);
            do
            {
                await SweepAsync(stoppingToken).ConfigureAwait(false);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    internal async Task SweepAsync(CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await runtime.DeleteOrphanedEphemeralAgentsAsync(AgentRuntime.MinimumOrphanAgeFor(runOptions), cancellationToken)
                .ConfigureAwait(false);
            if (deleted.Count > 0)
            {
                logger.LogInformation("Deleted {Count} orphaned ephemeral agent(s)", deleted.Count);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Orphaned ephemeral agent sweep failed; it will run again in {Interval}", interval);
        }
    }
}
