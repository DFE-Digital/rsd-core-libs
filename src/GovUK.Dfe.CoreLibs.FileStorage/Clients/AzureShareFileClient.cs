using Azure.Storage.Files.Shares;
using Azure.Storage.Sas;
using System.IO;

namespace GovUK.Dfe.CoreLibs.FileStorage.Clients;

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

    public async Task<string> GenerateSasUriAsync(DateTimeOffset expiresOn, string permissions, CancellationToken token = default)
    {
        // Parse permissions string to ShareSasPermissions
        var sasPermissions = new ShareFileSasPermissions();
        
        foreach (char p in permissions.ToLowerInvariant())
        {
            switch (p)
            {
                case 'r':
                    sasPermissions |= ShareFileSasPermissions.Read;
                    break;
                case 'w':
                    sasPermissions |= ShareFileSasPermissions.Write;
                    break;
                case 'd':
                    sasPermissions |= ShareFileSasPermissions.Delete;
                    break;
                case 'c':
                    sasPermissions |= ShareFileSasPermissions.Create;
                    break;
            }
        }

        // Create SAS builder
        var sasBuilder = new ShareSasBuilder
        {
            ShareName = _fileClient.ShareName,
            FilePath = _fileClient.Path,
            Resource = "f", // f = file
            ExpiresOn = expiresOn,
        };
        sasBuilder.SetPermissions(sasPermissions);

        // Generate the SAS URI
        var sasUri = _fileClient.GenerateSasUri(sasBuilder);
        return await Task.FromResult(sasUri.ToString());
    }
}
