using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using NSubstitute;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration.Fakes;

/// <summary>A blob container in memory: blobs, one lease per blob, and failures a real account can return.</summary>
internal sealed class InMemoryBlobContainer : BlobContainerClient
{
    private readonly object _lock = new();
    private readonly Dictionary<string, string?> _blobs = new(StringComparer.Ordinal);   // name -> lease id

    public override Uri Uri { get; } = new("https://account.blob.core.windows.net/aiagents-run-slots");

    public bool ContainerExists { get; set; }

    /// <summary>Every call fails with 403, as when the identity lacks Storage Blob Data Contributor.</summary>
    public bool Denied { get; set; }

    /// <summary>Every call fails as if the account can't be reached.</summary>
    public bool Unreachable { get; set; }

    public PublicAccessType? CreatedWith { get; private set; }

    public IReadOnlyCollection<string> BlobNames { get { lock (_lock) { return [.. _blobs.Keys]; } } }

    public bool IsLeased(string name) { lock (_lock) { return _blobs.GetValueOrDefault(name) is not null; } }

    public override BlobClient GetBlobClient(string blobName) => new Blob(this, blobName);

    public override Task<Response<bool>> ExistsAsync(CancellationToken cancellationToken = default)
    {
        Check();
        return Task.FromResult(Response.FromValue(ContainerExists, Substitute.For<Response>()));
    }

    public override Task<Response<BlobContainerInfo>> CreateAsync(PublicAccessType publicAccessType = PublicAccessType.None,
        IDictionary<string, string>? metadata = null, BlobContainerEncryptionScopeOptions? encryptionScopeOptions = null,
        CancellationToken cancellationToken = default)
    {
        Check();
        lock (_lock)
        {
            if (ContainerExists)
            {
                throw Error(409, BlobErrorCode.ContainerAlreadyExists);
            }

            ContainerExists = true;
            CreatedWith = publicAccessType;
        }

        return Task.FromResult(Response.FromValue(BlobsModelFactory.BlobContainerInfo(new ETag("c"), DateTimeOffset.UtcNow), Substitute.For<Response>()));
    }

    /// <summary>Removes the container and its blobs, as if someone deleted it after startup.</summary>
    public void Delete()
    {
        lock (_lock)
        {
            ContainerExists = false;
            _blobs.Clear();
        }
    }

    private void Check()
    {
        if (Unreachable)
        {
            throw new RequestFailedException("No such host is known.");
        }

        if (Denied)
        {
            throw Error(403, BlobErrorCode.AuthorizationPermissionMismatch);
        }
    }

    private static RequestFailedException Error(int status, BlobErrorCode code) => new(status, code.ToString(), code.ToString(), null);

    private sealed class Blob(InMemoryBlobContainer container, string name) : BlobClient
    {
#pragma warning disable S4275 // A fake: it has no SDK state for the base property to read.
        public override Uri Uri => new(container.Uri, $"{container.Uri.AbsolutePath}/{name}");
#pragma warning restore S4275

        public override string Name => name;

        public override Task<Response<BlobContentInfo>> UploadAsync(BinaryData content, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            container.Check();
            lock (container._lock)
            {
                if (!container.ContainerExists)
                {
                    throw Error(404, BlobErrorCode.ContainerNotFound);
                }

                if (container._blobs.ContainsKey(name) && !overwrite)
                {
                    throw Error(409, BlobErrorCode.BlobAlreadyExists);
                }

                container._blobs.TryAdd(name, null);
            }

            return Task.FromResult(Response.FromValue(
                BlobsModelFactory.BlobContentInfo(new ETag("b"), DateTimeOffset.UtcNow, null, null, null, null, 0), Substitute.For<Response>()));
        }

        protected override BlobLeaseClient GetBlobLeaseClientCore(string leaseId) => new Lease(container, this);
    }

    private sealed class Lease(InMemoryBlobContainer container, Blob blob) : BlobLeaseClient
    {
        private readonly string _id = Guid.NewGuid().ToString();

        public override Task<Response<BlobLease>> AcquireAsync(TimeSpan duration, RequestConditions? conditions = null,
            CancellationToken cancellationToken = default)
        {
            container.Check();
            lock (container._lock)
            {
                if (!container.ContainerExists)
                {
                    throw Error(404, BlobErrorCode.ContainerNotFound);
                }

                if (!container._blobs.TryGetValue(blob.Name, out var holder))
                {
                    throw Error(404, BlobErrorCode.BlobNotFound);
                }

                if (holder is not null && holder != _id)
                {
                    throw Error(409, BlobErrorCode.LeaseAlreadyPresent);
                }

                container._blobs[blob.Name] = _id;
            }

            return Task.FromResult(Response.FromValue(BlobsModelFactory.BlobLease(new ETag("l"), DateTimeOffset.UtcNow, _id), Substitute.For<Response>()));
        }

        public override Task<Response<BlobLease>> RenewAsync(RequestConditions? conditions = null, CancellationToken cancellationToken = default)
            => AcquireAsync(TimeSpan.FromSeconds(60), conditions, cancellationToken);

        public override Task<Response<ReleasedObjectInfo>> ReleaseAsync(RequestConditions? conditions = null, CancellationToken cancellationToken = default)
        {
            container.Check();
            lock (container._lock)
            {
                if (container._blobs.GetValueOrDefault(blob.Name) == _id)
                {
                    container._blobs[blob.Name] = null;
                }
            }

            return Task.FromResult(Response.FromValue(new ReleasedObjectInfo(new ETag("r"), DateTimeOffset.UtcNow), Substitute.For<Response>()));
        }
    }
}
