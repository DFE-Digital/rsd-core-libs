using GovUK.Dfe.CoreLibs.AiAgents.Concurrency;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration.Fakes;

/// <summary>Run slots shared by every limiter given this store, as the blob store is shared by every instance.</summary>
internal sealed class InMemoryRunSlotStore(int capacity) : IRunSlotStore
{
    private readonly bool[] _held = new bool[capacity];
    private readonly object _lock = new();
    private int _inUse;

    public int Capacity => capacity;

    /// <summary>The most slots ever held at once.</summary>
    public int PeakInUse { get; private set; }

    public Task<IAsyncDisposable?> TryAcquireAsync(int slot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (_held[slot])
            {
                return Task.FromResult<IAsyncDisposable?>(null);
            }

            _held[slot] = true;
            PeakInUse = Math.Max(PeakInUse, ++_inUse);
            return Task.FromResult<IAsyncDisposable?>(new Held(this, slot));
        }
    }

    private void Release(int slot)
    {
        lock (_lock)
        {
            _held[slot] = false;
            _inUse--;
        }
    }

    private sealed class Held(InMemoryRunSlotStore store, int slot) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            store.Release(slot);
            return ValueTask.CompletedTask;
        }
    }
}
