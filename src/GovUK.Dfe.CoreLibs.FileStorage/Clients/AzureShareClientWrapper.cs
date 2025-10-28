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

		// Ensure the share exists (safe for Azure Files)
		await _shareClient.CreateIfNotExistsAsync(cancellationToken: token);

		// Normalize and split the provided path into directory segments and file name
		var normalized = path.Replace('\\', '/').Trim('/');
		var fileName = System.IO.Path.GetFileName(normalized);
		var dirPath = normalized.Length > fileName.Length
			? normalized.Substring(0, normalized.Length - fileName.Length).TrimEnd('/')
			: string.Empty;

		// Start from the implicit root directory (must not be created explicitly)
		var directory = _shareClient.GetRootDirectoryClient();

		// Create subdirectories if any (skip root)
		if (!string.IsNullOrEmpty(dirPath))
		{
			foreach (var segment in dirPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
			{
				directory = directory.GetSubdirectoryClient(segment);
				await directory.CreateIfNotExistsAsync(cancellationToken: token);
			}
		}

		var fileClient = directory.GetFileClient(fileName);
		return new AzureShareFileClient(fileClient);
    }
}
