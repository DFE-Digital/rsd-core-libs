using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.FileStorage.Settings;
using DfE.CoreLibs.FileStorage.Clients;
using System.IO;

namespace DfE.CoreLibs.FileStorage.Services;

public class AzureFileStorageService : IFileStorageService
{
    private readonly IShareClientWrapper _clientWrapper;

    public AzureFileStorageService(FileStorageOptions options)
        : this(new AzureShareClientWrapper(options.Azure.ConnectionString, options.Azure.ShareName))
    {
    }

    internal AzureFileStorageService(IShareClientWrapper clientWrapper)
    {
        _clientWrapper = clientWrapper;
    }

    public async Task UploadAsync(string path, Stream content, CancellationToken token = default)
    {
        var fileClient = await _clientWrapper.GetFileClientAsync(path, token);
        await fileClient.CreateAsync(content.Length, token);
        await fileClient.UploadAsync(content, token);
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken token = default)
    {
        var fileClient = await _clientWrapper.GetFileClientAsync(path, token);
        return await fileClient.DownloadAsync(token);
    }

    public async Task DeleteAsync(string path, CancellationToken token = default)
    {
        var fileClient = await _clientWrapper.GetFileClientAsync(path, token);
        await fileClient.DeleteIfExistsAsync(token);
    }
}
