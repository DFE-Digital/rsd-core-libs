using Azure.Storage.Files.Shares;

namespace DfE.CoreLibs.FileStorage.Clients;

internal class AzureShareClientWrapper : IShareClientWrapper
{
    private readonly ShareClient _shareClient;

    public AzureShareClientWrapper(string connectionString, string shareName)
    {
        _shareClient = new ShareClient(connectionString, shareName);
    }

    public async Task<IShareFileClient> GetFileClientAsync(string path, CancellationToken token = default)
    {
        var directory = _shareClient.GetRootDirectoryClient();
        await directory.CreateIfNotExistsAsync(cancellationToken: token);
        var fileClient = directory.GetFileClient(path);
        return new AzureShareFileClient(fileClient);
    }
}
