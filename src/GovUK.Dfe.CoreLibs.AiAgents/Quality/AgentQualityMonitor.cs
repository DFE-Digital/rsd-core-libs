using GovUK.Dfe.CoreLibs.AiAgents.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace GovUK.Dfe.CoreLibs.AiAgents.Quality;

/// <summary>
/// Scores a sample of live runs in the background and records <c>aiagents.quality.score</c>. Runs never wait: samples
/// go into a bounded queue, and are dropped when it's full.
/// </summary>
internal sealed class AgentQualityMonitor(IAgentRunEvaluator evaluator, double sampleRate, AgentRunOptions runOptions,
    ILogger<AgentQualityMonitor> logger) : BackgroundService
{
    private readonly Channel<AgentRunSample> _queue = Channel.CreateBounded<AgentRunSample>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    /// <summary>Whether to score this run. Checked before building a sample, so unsampled runs cost nothing.</summary>
    public bool ShouldSample() => Random.Shared.NextDouble() < sampleRate;

    public void Enqueue(AgentRunSample sample) => _queue.Writer.TryWrite(sample);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var sample in _queue.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                await ScoreAsync(sample, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    internal async Task ScoreAsync(AgentRunSample sample, CancellationToken cancellationToken)
    {
        try
        {
            var scores = await evaluator.EvaluateAsync(sample, cancellationToken).ConfigureAwait(false);
            foreach (var (metric, score) in scores)
            {
                AgentTelemetry.QualityScore.Record(score,
                    new KeyValuePair<string, object?>(AgentTelemetry.ApplicationTag, runOptions.ApplicationName),
                    new KeyValuePair<string, object?>(AgentTelemetry.AgentNameTag, sample.AgentName),
                    new KeyValuePair<string, object?>(AgentTelemetry.AgentVersionTag, sample.AgentVersion),
                    new KeyValuePair<string, object?>(AgentTelemetry.QualityMetricTag, metric));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Couldn't score a run of {AgentName}", sample.AgentName);
        }
    }
}
