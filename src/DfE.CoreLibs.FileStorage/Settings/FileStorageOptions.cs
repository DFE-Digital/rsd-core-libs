namespace DfE.CoreLibs.FileStorage.Settings;

/// <summary>
/// Configuration settings for the file storage library.
/// </summary>
public class FileStorageOptions
{
    /// <summary>
    /// Name of the provider to use (e.g. "Azure").
    /// </summary>
    public string Provider { get; set; } = "";

    /// <summary>
    /// Azure File Service specific configuration.
    /// </summary>
    public AzureFileStorageOptions Azure { get; set; } = new();
}

/// <summary>
/// Settings required for the Azure file storage provider.
/// </summary>
public class AzureFileStorageOptions
{
    /// <summary>
    /// Connection string for the Azure storage account.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Name of the file share to use.
    /// </summary>
    public string ShareName { get; set; } = string.Empty;
}
