using System.Collections.Generic;
using DfE.CoreLibs.FileStorage;
using System.Linq;
using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.FileStorage.Services;
using DfE.CoreLibs.FileStorage.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DfE.CoreLibs.FileStorage.Tests.Services;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFileStorage_RegistersAzureImplementation_WhenProviderIsAzure()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["FileStorage:Provider"] = "Azure",
            ["FileStorage:Azure:ConnectionString"] = "UseDevelopmentStorage=true",
            ["FileStorage:Azure:ShareName"] = "files"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
        var services = new ServiceCollection();

        services.AddFileStorage(configuration);

        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IFileStorageService));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(AzureFileStorageService), descriptor!.ImplementationType);
    }

    [Fact]
    public void AddFileStorage_BindsOptions_FromConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["FileStorage:Provider"] = "Azure",
            ["FileStorage:Azure:ConnectionString"] = "UseDevelopmentStorage=true",
            ["FileStorage:Azure:ShareName"] = "files"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
        var services = new ServiceCollection();

        services.AddFileStorage(configuration);
        var provider = services.BuildServiceProvider();

        var options = provider.GetService<FileStorageOptions>();

        Assert.NotNull(options);
        Assert.Equal("Azure", options!.Provider);
        Assert.Equal("UseDevelopmentStorage=true", options.Azure.ConnectionString);
        Assert.Equal("files", options.Azure.ShareName);
    }

    [Fact]
    public void AddFileStorage_NoServiceRegistered_WhenProviderUnknown()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["FileStorage:Provider"] = "Unknown"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
        var services = new ServiceCollection();

        services.AddFileStorage(configuration);

        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IFileStorageService));

        Assert.Null(descriptor);
    }
}
