using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.Diagnostics;
using System.Diagnostics;
using System.Security.Cryptography;

namespace GovUK.Dfe.CoreLibs.AiAgents.Concurrency;

/// <summary>
/// Applies <see cref="AgentRunOptions.MaxConcurrency"/> across every caller on this instance, then the global
/// limit from an <see cref="IRunSlotStore"/> when one is registered. Waiting for both counts towards
/// <see cref="AgentRunOptions.MaxWaitForRunSlot"/>.
/// </summary>
internal sealed class AgentRunLimiter : IAgentRunLimiter, IDisposable
{
    // Tried per attempt, in random order, so waiting runs don't all poll every slot.
    private const int SlotsTriedPerAttempt = 8;
    private static readonly TimeSpan FirstRetryDelay = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan LongestRetryDelay = TimeSpan.FromSeconds(2);

    private readonly SemaphoreSlim? _local;
    private readonly IRunSlotStore? _global;
    private readonly TimeSpan _maxWait;
    private readonly string _applicationName;

    public AgentRunLimiter(AgentRunOptions runOptions, IRunSlotStore? globalSlots = null)
    {
        ArgumentNullException.ThrowIfNull(runOptions);

        _local = runOptions.MaxConcurrency is int max ? new SemaphoreSlim(max, max) : null;
        _global = globalSlots;
        _maxWait = runOptions.MaxWaitForRunSlot;
        _applicationName = runOptions.ApplicationName;
    }

    public async ValueTask<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        if (_local is null && _global is null)
        {
            return Slot.None;
        }

        var started = Stopwatch.GetTimestamp();
        using var wait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        wait.CancelAfter(_maxWait);

        try
        {
            if (_local is not null)
            {
                await _local.WaitAsync(wait.Token).ConfigureAwait(false);
            }

            try
            {
                var globalSlot = _global is null ? null : await AcquireGlobalAsync(_global, wait.Token).ConfigureAwait(false);
                return new Slot(_local, globalSlot);
            }
            catch
            {
                _local?.Release();
                throw;
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(string.Format(ErrorMessages.NoRunSlot, _maxWait), ex);
        }
        finally
        {
            AgentTelemetry.RunSlotWait.Record(Stopwatch.GetElapsedTime(started).TotalSeconds,
                new KeyValuePair<string, object?>(AgentTelemetry.ApplicationTag, _applicationName));
        }
    }

    public void Dispose() => _local?.Dispose();

    private static async Task<IAsyncDisposable> AcquireGlobalAsync(IRunSlotStore store, CancellationToken cancellationToken)
    {
        var delay = FirstRetryDelay;
        while (true)
        {
            foreach (var slot in PickSlots(store.Capacity))
            {
                if (await store.TryAcquireAsync(slot, cancellationToken).ConfigureAwait(false) is { } held)
                {
                    return held;
                }
            }

            // Jittered, growing back-off, so instances waiting together don't poll in step.
            var halfDelay = (int)(delay.TotalMilliseconds / 2);
            await Task.Delay(TimeSpan.FromMilliseconds(halfDelay + RandomNumberGenerator.GetInt32(halfDelay + 1)), cancellationToken)
                .ConfigureAwait(false);
            delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, LongestRetryDelay.Ticks));
        }
    }

    private static int[] PickSlots(int capacity)
    {
        var slots = Enumerable.Range(0, capacity).ToArray();
        RandomNumberGenerator.Shuffle(slots.AsSpan());
        return slots[..Math.Min(capacity, SlotsTriedPerAttempt)];
    }

    private sealed class Slot(SemaphoreSlim? local, IAsyncDisposable? global) : IAsyncDisposable
    {
        public static readonly Slot None = new(null, null);

        private int _released;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 1)
            {
                return;
            }

            try
            {
                if (global is not null)
                {
                    await global.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                local?.Release();
            }
        }
    }
}
