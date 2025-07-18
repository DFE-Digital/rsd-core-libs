namespace DfE.CoreLibs.FileStorage.Settings;

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