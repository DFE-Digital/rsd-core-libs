using System.Collections.Generic;
using DfE.CoreLibs.FileStorage;
using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.FileStorage.Services;
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

        var provider = services.BuildServiceProvider();

        var service = provider.GetService<IFileStorageService>();
        Assert.IsType<AzureFileStorageService>(service);
    }
}
