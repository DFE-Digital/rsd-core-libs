using Azure.Storage.Files.Shares;
using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.FileStorage.Settings;

namespace DfE.CoreLibs.FileStorage.Services;

public class AzureFileStorageService(FileStorageOptions options) : IFileStorageService
{
    private readonly ShareClient _shareClient = new(options.Azure.ConnectionString, options.Azure.ShareName);

    public async Task UploadAsync(string path, Stream content, CancellationToken token = default)
    {
        var directory = _shareClient.GetRootDirectoryClient();
        ShareFileClient file = directory.GetFileClient(path);
        await directory.CreateIfNotExistsAsync(cancellationToken: token);
        await file.CreateAsync(content.Length, cancellationToken: token);
        await file.UploadAsync(content, cancellationToken: token);
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken token = default)
    {
        var directory = _shareClient.GetRootDirectoryClient();
        ShareFileClient file = directory.GetFileClient(path);
        var response = await file.DownloadAsync(cancellationToken: token);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string path, CancellationToken token = default)
    {
        var directory = _shareClient.GetRootDirectoryClient();
        ShareFileClient file = directory.GetFileClient(path);
        await file.DeleteIfExistsAsync(cancellationToken: token);
    }
}
