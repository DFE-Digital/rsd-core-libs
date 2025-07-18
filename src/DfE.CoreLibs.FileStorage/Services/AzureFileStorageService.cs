using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.FileStorage.Settings;
using DfE.CoreLibs.FileStorage.Clients;
using System.IO;

namespace DfE.CoreLibs.FileStorage.Services;

/// <summary>
/// Azure File Service based implementation of <see cref="IFileStorageService"/>.
/// </summary>
public class AzureFileStorageService : IFileStorageService
{
    private readonly IShareClientWrapper _clientWrapper;

    /// <summary>
    /// Creates a new instance of the service using the provided configuration <paramref name="options"/>.
    /// </summary>
    /// <param name="options">Azure file storage configuration.</param>
    public AzureFileStorageService(FileStorageOptions options)
        : this(new AzureShareClientWrapper(options.Azure.ConnectionString, options.Azure.ShareName))
    {
    }

    /// <summary>
    /// Internal constructor used for testing with a custom client wrapper.
    /// </summary>
    internal AzureFileStorageService(IShareClientWrapper clientWrapper)
    {
        _clientWrapper = clientWrapper;
    }

    /// <inheritdoc />
    public async Task UploadAsync(string path, Stream content, CancellationToken token = default)
    {
        var fileClient = await _clientWrapper.GetFileClientAsync(path, token);
        await fileClient.CreateAsync(content.Length, token);
        await fileClient.UploadAsync(content, token);
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadAsync(string path, CancellationToken token = default)
    {
        var fileClient = await _clientWrapper.GetFileClientAsync(path, token);
        return await fileClient.DownloadAsync(token);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string path, CancellationToken token = default)
    {
        var fileClient = await _clientWrapper.GetFileClientAsync(path, token);
        await fileClient.DeleteIfExistsAsync(token);
    }
}
