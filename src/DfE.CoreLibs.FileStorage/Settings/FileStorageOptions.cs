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