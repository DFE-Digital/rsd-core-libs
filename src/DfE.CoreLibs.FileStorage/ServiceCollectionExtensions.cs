using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.FileStorage.Services;
using DfE.CoreLibs.FileStorage.Settings;
using DfE.CoreLibs.FileStorage.Exceptions;
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
    /// <exception cref="ArgumentNullException">Thrown when services or configuration is null.</exception>
    /// <exception cref="FileStorageConfigurationException">Thrown when configuration is invalid or provider is not supported.</exception>
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new FileStorageOptions();
        configuration.GetSection("FileStorage").Bind(options);

        // Validate configuration
        if (string.IsNullOrWhiteSpace(options.Provider))
            throw new FileStorageConfigurationException("FileStorage:Provider configuration is required.");

        services.AddSingleton(options);

        return options.Provider.ToLowerInvariant() switch
        {
            "azure" => ValidateAzureConfiguration(options) ? 
                services.AddSingleton<IFileStorageService, AzureFileStorageService>() : 
                throw new FileStorageConfigurationException("Invalid Azure File Storage configuration. ConnectionString and ShareName are required."),
            "local" => ValidateLocalConfiguration(options) ? 
                services.AddSingleton<IFileStorageService, LocalFileStorageService>() : 
                throw new FileStorageConfigurationException("Invalid Local File Storage configuration."),
            _ => throw new FileStorageConfigurationException($"Unsupported file storage provider: {options.Provider}")
        };
    }

    private static bool ValidateAzureConfiguration(FileStorageOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.Azure.ConnectionString) && 
               !string.IsNullOrWhiteSpace(options.Azure.ShareName);
    }

    private static bool ValidateLocalConfiguration(FileStorageOptions options)
    {
        // Local configuration is always valid as it has sensible defaults
        return true;
    }
}
