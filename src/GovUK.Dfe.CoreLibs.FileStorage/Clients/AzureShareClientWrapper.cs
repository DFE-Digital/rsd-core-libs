using Azure.Storage.Files.Shares;

namespace GovUK.Dfe.CoreLibs.FileStorage.Clients;

internal class AzureShareClientWrapper(string connectionString, string shareName) : IShareClientWrapper
{
    private readonly ShareClient _shareClient = CreateShareClient(connectionString, shareName);

    private static ShareClient CreateShareClient(string connectionString, string shareName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null, empty, or whitespace.", nameof(connectionString));

        if (string.IsNullOrWhiteSpace(shareName))
            throw new ArgumentException("Share name cannot be null, empty, or whitespace.", nameof(shareName));

        // Basic validation for connection string format
        if (!connectionString.Contains("AccountName=") || !connectionString.Contains("AccountKey="))
            throw new ArgumentException("Invalid connection string format. Must contain AccountName and AccountKey.", nameof(connectionString));

        // Basic validation for share name format (Azure file share naming rules)
        if (shareName.Length < 3 || shareName.Length > 63)
            throw new ArgumentException("Share name must be between 3 and 63 characters long.", nameof(shareName));

        if (!shareName.All(c => char.IsLetterOrDigit(c) || c == '-'))
            throw new ArgumentException("Share name can only contain letters, numbers, and hyphens.", nameof(shareName));

        if (shareName.StartsWith('-') || shareName.EndsWith('-'))
            throw new ArgumentException("Share name cannot start or end with a hyphen.", nameof(shareName));

        return new ShareClient(connectionString, shareName);
    }

    public async Task<IShareFileClient> GetFileClientAsync(string path, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(path);

        var directory = _shareClient.GetRootDirectoryClient();
        await directory.CreateIfNotExistsAsync(cancellationToken: token);
        var fileClient = directory.GetFileClient(path);
        return new AzureShareFileClient(fileClient);
    }
}
