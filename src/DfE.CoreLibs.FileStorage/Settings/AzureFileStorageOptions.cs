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

    /// <summary>
    /// Optional timeout for Azure operations in seconds. Default is 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Optional retry policy configuration.
    /// </summary>
    public RetryPolicyOptions RetryPolicy { get; set; } = new();
}

/// <summary>
/// Configuration for retry policies.
/// </summary>
public class RetryPolicyOptions
{
    /// <summary>
    /// Maximum number of retry attempts. Default is 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay between retries in seconds. Default is 1 second.
    /// </summary>
    public double BaseDelaySeconds { get; set; } = 1.0;

    /// <summary>
    /// Maximum delay between retries in seconds. Default is 10 seconds.
    /// </summary>
    public double MaxDelaySeconds { get; set; } = 10.0;
}