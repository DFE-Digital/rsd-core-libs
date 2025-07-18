namespace DfE.CoreLibs.FileStorage.Settings;

public class FileStorageOptions
{
    public string Provider { get; set; } = "";
    public AzureFileStorageOptions Azure { get; set; } = new();
}

public class AzureFileStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ShareName { get; set; } = string.Empty;
}
