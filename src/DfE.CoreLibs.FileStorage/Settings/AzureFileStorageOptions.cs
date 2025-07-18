namespace DfE.CoreLibs.FileStorage.Settings;

public class AzureFileStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ShareName { get; set; } = string.Empty;
}