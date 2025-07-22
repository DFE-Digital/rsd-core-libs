using DfE.CoreLibs.FileStorage.Settings;
using Xunit;

namespace DfE.CoreLibs.FileStorage.Tests;

public class SettingsTests
{
    [Fact]
    public void FileStorageOptions_DefaultConstructor_ShouldCreateWithDefaults()
    {
        // Act
        var options = new FileStorageOptions();

        // Assert
        Assert.NotNull(options);
        Assert.Equal("", options.Provider);
        Assert.NotNull(options.Azure);
        Assert.IsType<AzureFileStorageOptions>(options.Azure);
    }

    [Fact]
    public void FileStorageOptions_WithValues_ShouldSetValues()
    {
        // Arrange
        var provider = "Azure";
        var connectionString = "test-connection-string";
        var shareName = "test-share";

        // Act
        var options = new FileStorageOptions
        {
            Provider = provider,
            Azure = new AzureFileStorageOptions
            {
                ConnectionString = connectionString,
                ShareName = shareName
            }
        };

        // Assert
        Assert.Equal(provider, options.Provider);
        Assert.Equal(connectionString, options.Azure.ConnectionString);
        Assert.Equal(shareName, options.Azure.ShareName);
    }

    [Fact]
    public void AzureFileStorageOptions_DefaultConstructor_ShouldCreateWithDefaults()
    {
        // Act
        var options = new AzureFileStorageOptions();

        // Assert
        Assert.NotNull(options);
        Assert.Equal("", options.ConnectionString);
        Assert.Equal("", options.ShareName);
        Assert.Equal(30, options.TimeoutSeconds);
        Assert.NotNull(options.RetryPolicy);
        Assert.IsType<RetryPolicyOptions>(options.RetryPolicy);
    }

    [Fact]
    public void AzureFileStorageOptions_WithValues_ShouldSetValues()
    {
        // Arrange
        var connectionString = "test-connection-string";
        var shareName = "test-share";
        var timeoutSeconds = 60;

        // Act
        var options = new AzureFileStorageOptions
        {
            ConnectionString = connectionString,
            ShareName = shareName,
            TimeoutSeconds = timeoutSeconds
        };

        // Assert
        Assert.Equal(connectionString, options.ConnectionString);
        Assert.Equal(shareName, options.ShareName);
        Assert.Equal(timeoutSeconds, options.TimeoutSeconds);
    }

    [Fact]
    public void RetryPolicyOptions_DefaultConstructor_ShouldCreateWithDefaults()
    {
        // Act
        var options = new RetryPolicyOptions();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(3, options.MaxRetries);
        Assert.Equal(1.0, options.BaseDelaySeconds);
        Assert.Equal(10.0, options.MaxDelaySeconds);
    }

    [Fact]
    public void RetryPolicyOptions_WithValues_ShouldSetValues()
    {
        // Arrange
        var maxRetries = 5;
        var baseDelaySeconds = 2.0;
        var maxDelaySeconds = 20.0;

        // Act
        var options = new RetryPolicyOptions
        {
            MaxRetries = maxRetries,
            BaseDelaySeconds = baseDelaySeconds,
            MaxDelaySeconds = maxDelaySeconds
        };

        // Assert
        Assert.Equal(maxRetries, options.MaxRetries);
        Assert.Equal(baseDelaySeconds, options.BaseDelaySeconds);
        Assert.Equal(maxDelaySeconds, options.MaxDelaySeconds);
    }

    [Fact]
    public void AzureFileStorageOptions_WithRetryPolicy_ShouldSetRetryPolicy()
    {
        // Arrange
        var retryPolicy = new RetryPolicyOptions
        {
            MaxRetries = 7,
            BaseDelaySeconds = 3.0,
            MaxDelaySeconds = 30.0
        };

        // Act
        var options = new AzureFileStorageOptions
        {
            ConnectionString = "test",
            ShareName = "test",
            RetryPolicy = retryPolicy
        };

        // Assert
        Assert.NotNull(options.RetryPolicy);
        Assert.Equal(7, options.RetryPolicy.MaxRetries);
        Assert.Equal(3.0, options.RetryPolicy.BaseDelaySeconds);
        Assert.Equal(30.0, options.RetryPolicy.MaxDelaySeconds);
    }

    [Fact]
    public void FileStorageOptions_WithCompleteConfiguration_ShouldSetAllValues()
    {
        // Arrange
        var provider = "Azure";
        var connectionString = "test-connection-string";
        var shareName = "test-share";
        var timeoutSeconds = 45;
        var maxRetries = 4;
        var baseDelaySeconds = 1.5;
        var maxDelaySeconds = 15.0;

        // Act
        var options = new FileStorageOptions
        {
            Provider = provider,
            Azure = new AzureFileStorageOptions
            {
                ConnectionString = connectionString,
                ShareName = shareName,
                TimeoutSeconds = timeoutSeconds,
                RetryPolicy = new RetryPolicyOptions
                {
                    MaxRetries = maxRetries,
                    BaseDelaySeconds = baseDelaySeconds,
                    MaxDelaySeconds = maxDelaySeconds
                }
            }
        };

        // Assert
        Assert.Equal(provider, options.Provider);
        Assert.Equal(connectionString, options.Azure.ConnectionString);
        Assert.Equal(shareName, options.Azure.ShareName);
        Assert.Equal(timeoutSeconds, options.Azure.TimeoutSeconds);
        Assert.Equal(maxRetries, options.Azure.RetryPolicy.MaxRetries);
        Assert.Equal(baseDelaySeconds, options.Azure.RetryPolicy.BaseDelaySeconds);
        Assert.Equal(maxDelaySeconds, options.Azure.RetryPolicy.MaxDelaySeconds);
    }
} 