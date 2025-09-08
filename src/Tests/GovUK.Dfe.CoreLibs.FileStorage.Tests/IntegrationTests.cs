using GovUK.Dfe.CoreLibs.FileStorage;
using GovUK.Dfe.CoreLibs.FileStorage.Interfaces;
using GovUK.Dfe.CoreLibs.FileStorage.Services;
using GovUK.Dfe.CoreLibs.FileStorage.Settings;
using GovUK.Dfe.CoreLibs.FileStorage.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GovUK.Dfe.CoreLibs.FileStorage.Tests;

public class IntegrationTests
{
    [Fact]
    public void FullIntegration_WithValidConfiguration_ShouldWorkEndToEnd()
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
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var fileStorageService = serviceProvider.GetService<IFileStorageService>();
        Assert.NotNull(fileStorageService);
        Assert.IsType<AzureFileStorageService>(fileStorageService);

        var options = serviceProvider.GetService<FileStorageOptions>();
        Assert.NotNull(options);
        Assert.Equal("Azure", options.Provider);
    }

    [Fact]
    public void ServiceLifetime_ShouldBeSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Azure",
            ["FileStorage:Azure:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net",
            ["FileStorage:Azure:ShareName"] = "testshare"
        });

        services.AddFileStorage(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var service1 = serviceProvider.GetService<IFileStorageService>();
        var service2 = serviceProvider.GetService<IFileStorageService>();

        // Assert
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Same(service1, service2); // Should be the same instance (singleton)
    }

    [Fact]
    public void ConfigurationBinding_ShouldBindAllProperties()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Azure",
            ["FileStorage:Azure:ConnectionString"] = "test-connection-string",
            ["FileStorage:Azure:ShareName"] = "test-share-name",
            ["FileStorage:Azure:TimeoutSeconds"] = "60",
            ["FileStorage:Azure:RetryPolicy:MaxRetries"] = "5",
            ["FileStorage:Azure:RetryPolicy:BaseDelaySeconds"] = "2.0",
            ["FileStorage:Azure:RetryPolicy:MaxDelaySeconds"] = "20.0"
        });

        // Act
        services.AddFileStorage(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<FileStorageOptions>();

        // Assert
        Assert.NotNull(options);
        Assert.Equal("Azure", options.Provider);
        Assert.Equal("test-connection-string", options.Azure.ConnectionString);
        Assert.Equal("test-share-name", options.Azure.ShareName);
        Assert.Equal(60, options.Azure.TimeoutSeconds);
        Assert.Equal(5, options.Azure.RetryPolicy.MaxRetries);
        Assert.Equal(2.0, options.Azure.RetryPolicy.BaseDelaySeconds);
        Assert.Equal(20.0, options.Azure.RetryPolicy.MaxDelaySeconds);
    }

    [Fact]
    public void ServiceRegistration_WithInvalidConfiguration_ShouldThrowAtRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "InvalidProvider"
        });

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => services.AddFileStorage(configuration));
        Assert.Contains("Unsupported file storage provider: InvalidProvider", exception.Message);
    }

    [Fact]
    public void ServiceRegistration_WithMissingAzureConfiguration_ShouldThrowAtRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Azure"
            // Missing Azure configuration
        });

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => services.AddFileStorage(configuration));
        Assert.Contains("ConnectionString and ShareName are required", exception.Message);
    }

    [Fact]
    public void ServiceRegistration_WithEmptyProvider_ShouldThrowAtRegistration()
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
    public void ServiceRegistration_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddFileStorage(null!));
    }

    [Fact]
    public void ServiceRegistration_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var configuration = CreateConfiguration(new Dictionary<string, string>());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddFileStorage(configuration));
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string> settings)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(settings);
        return configurationBuilder.Build();
    }
}
