using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.FileStorage.Services;
using DfE.CoreLibs.FileStorage.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DfE.CoreLibs.FileStorage;

/// <summary>
/// Extension methods for registering the file storage services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="IFileStorageService"/> based on configuration under the
    /// <c>FileStorage</c> section.
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The original <paramref name="services"/> instance.</returns>
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
