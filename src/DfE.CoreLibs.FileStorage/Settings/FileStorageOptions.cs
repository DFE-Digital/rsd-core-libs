namespace DfE.CoreLibs.FileStorage.Settings;

public class FileStorageOptions
{
    public string Provider { get; set; } = "";
    public AzureFileStorageOptions Azure { get; set; } = new();
}