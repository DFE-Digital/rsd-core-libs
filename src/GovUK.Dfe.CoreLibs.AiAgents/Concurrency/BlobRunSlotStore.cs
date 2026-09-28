using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GovUK.Dfe.CoreLibs.AiAgents.Concurrency;

/// <summary>
/// Run slots as blob leases: one empty blob per slot, created on first use. A held lease is renewed while the
/// run lasts; if the instance dies, the lease expires within <see cref="LeaseDuration"/> and the slot frees itself.
/// </summary>
internal sealed class BlobRunSlotStore(BlobContainerClient container, int capacity, ILogger<BlobRunSlotStore>? logger = null)
    : IRunSlotStore
{
    internal static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RenewEvery = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ReleaseTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger<BlobRunSlotStore> _logger = logger ?? NullLogger<BlobRunSlotStore>.Instance;

    public int Capacity => capacity;

    public async Task<IAsyncDisposable?> TryAcquireAsync(int slot, CancellationToken cancellationToken = default)
    {
        var blob = container.GetBlobClient($"run-slot-{slot:D4}");
        var lease = blob.GetBlobLeaseClient();

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                await lease.AcquireAsync(LeaseDuration, cancellationToken: cancellationToken).ConfigureAwait(false);
                return new HeldSlot(lease, _logger);
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.LeaseAlreadyPresent)
            {
                return null;
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.ContainerNotFound)
            {
                throw new InvalidOperationException(string.Format(ErrorMessages.RunSlotContainerNotFound, container.Uri), ex);
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound && attempt == 1)
            {
                await CreateSlotAsync(blob, cancellationToken).ConfigureAwait(false);
            }
        }

        return null;
    }

    private static async Task CreateSlotAsync(BlobClient blob, CancellationToken cancellationToken)
    {
        try
        {
            await blob.UploadAsync(BinaryData.Empty, overwrite: false, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobAlreadyExists)
        {
            // Another instance created it first.
        }
    }

    private sealed class HeldSlot : IAsyncDisposable
    {
        private readonly BlobLeaseClient _lease;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _released = new();
        private readonly Task _renewal;

        public HeldSlot(BlobLeaseClient lease, ILogger logger)
        {
            _lease = lease;
            _logger = logger;
            _renewal = RenewUntilReleasedAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _released.CancelAsync().ConfigureAwait(false);
            await _renewal.ConfigureAwait(false);

            using var timeout = new CancellationTokenSource(ReleaseTimeout);
            try
            {
                await _lease.ReleaseAsync(cancellationToken: timeout.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is RequestFailedException or OperationCanceledException)
            {
                _logger.LogWarning(ex, "Couldn't release run slot {Slot}; it frees itself when its lease expires", _lease.Uri);
            }
            finally
            {
                _released.Dispose();
            }
        }

        private async Task RenewUntilReleasedAsync()
        {
            using var timer = new PeriodicTimer(RenewEvery);
            try
            {
                while (await timer.WaitForNextTickAsync(_released.Token).ConfigureAwait(false))
                {
                    await RenewAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Released.
            }
        }

        private async Task RenewAsync()
        {
            try
            {
                await _lease.RenewAsync(cancellationToken: _released.Token).ConfigureAwait(false);
            }
            catch (RequestFailedException ex)
            {
                // The run carries on; if the lease has lapsed, another run may briefly share this slot.
                _logger.LogWarning(ex, "Couldn't renew run slot {Slot}", _lease.Uri);
            }
        }
    }
}
