using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;

namespace GovUK.Dfe.CoreLibs.AiAgents.Concurrency;

/// <summary>
/// Run slots as blob leases: one empty blob per slot, created on first use. A held lease is renewed while the
/// run lasts; if the instance dies, the lease expires within <see cref="LeaseDuration"/> and the slot frees itself.
/// The container is created, private, if it's missing.
/// </summary>
internal sealed class BlobRunSlotStore(BlobContainerClient container, int capacity, ILogger<BlobRunSlotStore>? logger = null)
    : IRunSlotStore
{
    internal static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(60);
    internal const string AccessCheckBlob = "access-check";
    private static readonly TimeSpan RenewEvery = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ReleaseTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan AccessCheckLease = TimeSpan.FromSeconds(15);

    private readonly ILogger<BlobRunSlotStore> _logger = logger ?? NullLogger<BlobRunSlotStore>.Instance;

    public int Capacity => capacity;

    public Uri ContainerUri => container.Uri;

    /// <summary>Creates the container if it's missing, then proves this identity can write and lease blobs in it.</summary>
    /// <exception cref="InvalidOperationException">The identity lacks Storage Blob Data Contributor.</exception>
    public async Task EnsureAccessAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!(await container.ExistsAsync(cancellationToken).ConfigureAwait(false)).Value)
            {
                await CreateContainerAsync(cancellationToken).ConfigureAwait(false);
            }

            var probe = container.GetBlobClient(AccessCheckBlob);
            await probe.UploadAsync(BinaryData.Empty, overwrite: true, cancellationToken).ConfigureAwait(false);
            var lease = probe.GetBlobLeaseClient();
            await lease.AcquireAsync(AccessCheckLease, cancellationToken: cancellationToken).ConfigureAwait(false);
            await lease.ReleaseAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Forbidden)
        {
            throw AccessDenied(ex);
        }
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(int slot, CancellationToken cancellationToken = default)
    {
        var blob = container.GetBlobClient($"run-slot-{slot:D4}");
        var lease = blob.GetBlobLeaseClient();

        // At most: create the container, then the slot's blob, then take the lease.
        for (var attempt = 1; attempt <= 3; attempt++)
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
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Forbidden)
            {
                throw AccessDenied(ex);
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.ContainerNotFound && attempt < 3)
            {
                await CreateContainerAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound && attempt < 3)
            {
                await CreateSlotAsync(blob, cancellationToken).ConfigureAwait(false);
            }
        }

        return null;
    }

    private async Task CreateContainerAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Private: no anonymous access, whatever the account allows.
            await container.CreateAsync(PublicAccessType.None, cancellationToken: cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Created the run slot container {Container}", container.Uri);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.ContainerAlreadyExists)
        {
            // Another instance created it first.
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Forbidden)
        {
            throw AccessDenied(ex);
        }
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

    private InvalidOperationException AccessDenied(RequestFailedException ex)
        => new(string.Format(ErrorMessages.RunSlotAccessDenied, container.Uri), ex);

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
