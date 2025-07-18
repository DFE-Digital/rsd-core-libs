using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.FileStorage.Services;
using DfE.CoreLibs.FileStorage.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DfE.CoreLibs.FileStorage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new FileStorageOptions();
        configuration.GetSection("FileStorage").Bind(options);
        services.AddSingleton(options);

        return options.Provider.ToLower() switch
        {
            "azure" => services.AddSingleton<IFileStorageService, AzureFileStorageService>(),
            _ => services
        };
    }
}
