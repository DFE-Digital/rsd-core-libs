using DfE.CoreLibs.FileStorage;
using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.FileStorage.Services;
using DfE.CoreLibs.FileStorage.Settings;
using DfE.CoreLibs.FileStorage.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DfE.CoreLibs.FileStorage.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFileStorage_WithValidAzureConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Azure",
            ["FileStorage:Azure:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net",
            ["FileStorage:Azure:ShareName"] = "testshare"
        });

        // Act
        services.AddFileStorage(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var fileStorageService = serviceProvider.GetService<IFileStorageService>();
        var options = serviceProvider.GetService<FileStorageOptions>();

        Assert.NotNull(fileStorageService);
        Assert.IsType<AzureFileStorageService>(fileStorageService);
        Assert.NotNull(options);
        Assert.Equal("Azure", options.Provider);
        Assert.Equal("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net", options.Azure.ConnectionString);
        Assert.Equal("testshare", options.Azure.ShareName);
    }

    [Fact]
    public void AddFileStorage_WithValidLocalConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var testDirectory = Path.Combine(Path.GetTempPath(), "TestFileStorage", Guid.NewGuid().ToString());
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Local",
            ["FileStorage:Local:BaseDirectory"] = testDirectory,
            ["FileStorage:Local:CreateDirectoryIfNotExists"] = "true",
            ["FileStorage:Local:AllowOverwrite"] = "true",
            ["FileStorage:Local:MaxFileSizeBytes"] = "104857600"
        });

        try
        {
            // Act
            services.AddFileStorage(configuration);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var fileStorageService = serviceProvider.GetService<IFileStorageService>();
            var options = serviceProvider.GetService<FileStorageOptions>();

            Assert.NotNull(fileStorageService);
            Assert.IsType<LocalFileStorageService>(fileStorageService);
            Assert.NotNull(options);
            Assert.Equal("Local", options.Provider);
            Assert.Equal(testDirectory, options.Local.BaseDirectory);
            Assert.True(options.Local.CreateDirectoryIfNotExists);
            Assert.True(options.Local.AllowOverwrite);
            Assert.Equal(104857600, options.Local.MaxFileSizeBytes);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    [Fact]
    public void AddFileStorage_WithLocalProviderCaseInsensitive_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var testDirectory = Path.Combine(Path.GetTempPath(), "TestFileStorage", Guid.NewGuid().ToString());
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "LOCAL",
            ["FileStorage:Local:BaseDirectory"] = testDirectory
        });

        try
        {
            // Act
            services.AddFileStorage(configuration);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var fileStorageService = serviceProvider.GetService<IFileStorageService>();

            Assert.NotNull(fileStorageService);
            Assert.IsType<LocalFileStorageService>(fileStorageService);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    [Fact]
    public void AddFileStorage_WithLocalProviderMixedCase_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var testDirectory = Path.Combine(Path.GetTempPath(), "TestFileStorage", Guid.NewGuid().ToString());
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Local",
            ["FileStorage:Local:BaseDirectory"] = testDirectory
        });

        try
        {
            // Act
            services.AddFileStorage(configuration);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var fileStorageService = serviceProvider.GetService<IFileStorageService>();

            Assert.NotNull(fileStorageService);
            Assert.IsType<LocalFileStorageService>(fileStorageService);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    [Fact]
    public void AddFileStorage_WithLocalProviderAndDefaultSettings_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Local"
        });

        // Act
        services.AddFileStorage(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var fileStorageService = serviceProvider.GetService<IFileStorageService>();
        var options = serviceProvider.GetService<FileStorageOptions>();

        Assert.NotNull(fileStorageService);
        Assert.IsType<LocalFileStorageService>(fileStorageService);
        Assert.NotNull(options);
        Assert.Equal("Local", options.Provider);
        Assert.True(options.Local.CreateDirectoryIfNotExists);
        Assert.True(options.Local.AllowOverwrite);
        Assert.Equal(100 * 1024 * 1024, options.Local.MaxFileSizeBytes);
    }

    [Fact]
    public void AddFileStorage_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var configuration = CreateConfiguration(new Dictionary<string, string>());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddFileStorage(configuration));
    }

    [Fact]
    public void AddFileStorage_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration configuration = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddFileStorage(configuration));
    }

    [Fact]
    public void AddFileStorage_WithMissingProvider_ShouldThrowFileStorageConfigurationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>());

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => services.AddFileStorage(configuration));
        Assert.Contains("Provider configuration is required", exception.Message);
    }

    [Fact]
    public void AddFileStorage_WithEmptyProvider_ShouldThrowFileStorageConfigurationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = ""
        });

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => services.AddFileStorage(configuration));
        Assert.Contains("Provider configuration is required", exception.Message);
    }

    [Fact]
    public void AddFileStorage_WithWhitespaceProvider_ShouldThrowFileStorageConfigurationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "   "
        });

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => services.AddFileStorage(configuration));
        Assert.Contains("Provider configuration is required", exception.Message);
    }

    [Fact]
    public void AddFileStorage_WithUnsupportedProvider_ShouldThrowFileStorageConfigurationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "UnsupportedProvider"
        });

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => services.AddFileStorage(configuration));
        Assert.Contains("Unsupported file storage provider: UnsupportedProvider", exception.Message);
    }

    [Fact]
    public void AddFileStorage_WithAzureProviderButMissingConnectionString_ShouldThrowFileStorageConfigurationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Azure",
            ["FileStorage:Azure:ShareName"] = "testshare"
        });

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => services.AddFileStorage(configuration));
        Assert.Contains("ConnectionString and ShareName are required", exception.Message);
    }

    [Fact]
    public void AddFileStorage_WithAzureProviderButMissingShareName_ShouldThrowFileStorageConfigurationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Azure",
            ["FileStorage:Azure:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net"
        });

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => services.AddFileStorage(configuration));
        Assert.Contains("ConnectionString and ShareName are required", exception.Message);
    }

    [Fact]
    public void AddFileStorage_WithAzureProviderButEmptyConnectionString_ShouldThrowFileStorageConfigurationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Azure",
            ["FileStorage:Azure:ConnectionString"] = "",
            ["FileStorage:Azure:ShareName"] = "testshare"
        });

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => services.AddFileStorage(configuration));
        Assert.Contains("ConnectionString and ShareName are required", exception.Message);
    }

    [Fact]
    public void AddFileStorage_WithAzureProviderButEmptyShareName_ShouldThrowFileStorageConfigurationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Azure",
            ["FileStorage:Azure:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net",
            ["FileStorage:Azure:ShareName"] = ""
        });

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => services.AddFileStorage(configuration));
        Assert.Contains("ConnectionString and ShareName are required", exception.Message);
    }

    [Fact]
    public void AddFileStorage_WithAzureProviderCaseInsensitive_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "AZURE",
            ["FileStorage:Azure:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net",
            ["FileStorage:Azure:ShareName"] = "testshare"
        });

        // Act
        services.AddFileStorage(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var fileStorageService = serviceProvider.GetService<IFileStorageService>();

        Assert.NotNull(fileStorageService);
        Assert.IsType<AzureFileStorageService>(fileStorageService);
    }

    [Fact]
    public void AddFileStorage_WithAzureProviderMixedCase_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Azure",
            ["FileStorage:Azure:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net",
            ["FileStorage:Azure:ShareName"] = "testshare"
        });

        // Act
        services.AddFileStorage(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var fileStorageService = serviceProvider.GetService<IFileStorageService>();

        Assert.NotNull(fileStorageService);
        Assert.IsType<AzureFileStorageService>(fileStorageService);
    }

    [Fact]
    public void AddFileStorage_WithAdditionalAzureOptions_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Azure",
            ["FileStorage:Azure:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net",
            ["FileStorage:Azure:ShareName"] = "testshare",
            ["FileStorage:Azure:TimeoutSeconds"] = "60",
            ["FileStorage:Azure:RetryPolicy:MaxRetries"] = "5",
            ["FileStorage:Azure:RetryPolicy:BaseDelaySeconds"] = "2.0",
            ["FileStorage:Azure:RetryPolicy:MaxDelaySeconds"] = "20.0"
        });

        // Act
        services.AddFileStorage(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<FileStorageOptions>();

        Assert.NotNull(options);
        Assert.Equal(60, options.Azure.TimeoutSeconds);
        Assert.Equal(5, options.Azure.RetryPolicy.MaxRetries);
        Assert.Equal(2.0, options.Azure.RetryPolicy.BaseDelaySeconds);
        Assert.Equal(20.0, options.Azure.RetryPolicy.MaxDelaySeconds);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string> settings)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(settings);
        return configurationBuilder.Build();
    }
}