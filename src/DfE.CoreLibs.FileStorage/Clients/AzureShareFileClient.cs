using Azure.Storage.Files.Shares;
using System.IO;

namespace DfE.CoreLibs.FileStorage.Clients;

internal class AzureShareFileClient(ShareFileClient fileClient) : IShareFileClient
{
    private readonly ShareFileClient _fileClient = fileClient;

    public async Task CreateAsync(long size, CancellationToken token = default)
    {
        await _fileClient.CreateAsync(size, cancellationToken: token);
    }

    public async Task UploadAsync(Stream content, CancellationToken token = default)
    {
        await _fileClient.UploadAsync(content, cancellationToken: token);
    }

    public async Task<Stream> DownloadAsync(CancellationToken token = default)
    {
        var response = await _fileClient.DownloadAsync(cancellationToken: token);
        return response.Value.Content;
    }

    public async Task DeleteIfExistsAsync(CancellationToken token = default)
    {
        await _fileClient.DeleteIfExistsAsync(cancellationToken: token);
    }

    public async Task<bool> ExistsAsync(CancellationToken token = default)
    {
        var response = await _fileClient.ExistsAsync(cancellationToken: token);
        return response.Value;
    }
}
