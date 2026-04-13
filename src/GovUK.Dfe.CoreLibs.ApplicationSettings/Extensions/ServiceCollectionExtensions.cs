using GovUK.Dfe.CoreLibs.ApplicationSettings.Configuration;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Data;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Interfaces;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Extensions;

public static class ServiceCollectionExtensions
{
    // Group all AddApplicationSettings overloads together
    public static IServiceCollection AddApplicationSettings(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DefaultConnection",
        string? schema = null)
    {
        // Configure options using shared method
        services.ConfigureApplicationSettingsOptions(configuration, schema);

        // Add DbContext
        services.AddDbContext<ApplicationSettingsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString(connectionStringName)));

        // Add shared dependencies and service
        return services.AddApplicationSettingsCore<ApplicationSettingsService>();
    }

    public static IServiceCollection AddApplicationSettings(
        this IServiceCollection services,
        string connectionString,
        Action<ApplicationSettingsOptions>? configureOptions = null)
    {
        // Configure options with defaults and optional customization
        services.Configure<ApplicationSettingsOptions>(options =>
        {
            // Set defaults using shared method
            SetDefaultOptions(options);

            // Apply custom configuration if provided
            configureOptions?.Invoke(options);
        });

        // Add DbContext
        services.AddDbContext<ApplicationSettingsDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Add shared dependencies and service
        return services.AddApplicationSettingsCore<ApplicationSettingsService>();
    }

    // Now place the different method after all AddApplicationSettings overloads
    /// <summary>
    /// Adds ApplicationSettings service using an existing DbContext
    /// </summary>
    /// <typeparam name="TContext">The existing DbContext type</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="schema">Optional schema override</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddApplicationSettingsWithExistingContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? schema = null)
        where TContext : DbContext
    {
        // Configure options using shared method
        services.ConfigureApplicationSettingsOptions(configuration, schema);

        // Add shared dependencies and service
        return services.AddApplicationSettingsCore<ExistingContextApplicationSettingsService<TContext>>();
    }

    // All private helper methods remain the same...
    /// <summary>
    /// Configures ApplicationSettingsOptions with defaults and reads from configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="schema">Optional schema override</param>
    private static void ConfigureApplicationSettingsOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        string? schema = null)
    {
        services.Configure<ApplicationSettingsOptions>(options =>
        {
            // Set defaults
            SetDefaultOptions(options, schema);

            // Read from configuration section if it exists
            var section = configuration.GetSection(ApplicationSettingsOptions.ConfigurationSection);
            if (section.Exists())
            {
                ApplyConfigurationSettings(options, section, schema);
            }
        });
    }

    /// <summary>
    /// Sets default values for ApplicationSettingsOptions
    /// </summary>
    /// <param name="options">Options to configure</param>
    /// <param name="schema">Optional schema override</param>
    private static void SetDefaultOptions(ApplicationSettingsOptions options, string? schema = null)
    {
        options.EnableCaching = true;
        options.CacheExpirationMinutes = 30;
        options.DefaultCategory = "General";
        options.Schema = schema;
    }

    /// <summary>
    /// Applies configuration settings from IConfiguration section
    /// </summary>
    /// <param name="options">Options to configure</param>
    /// <param name="section">Configuration section</param>
    /// <param name="schema">Optional schema override</param>
    private static void ApplyConfigurationSettings(
        ApplicationSettingsOptions options,
        IConfiguration section,
        string? schema = null)
    {
        if (bool.TryParse(section["EnableCaching"], out bool enableCaching))
            options.EnableCaching = enableCaching;

        if (int.TryParse(section["CacheExpirationMinutes"], out int cacheExpiration))
            options.CacheExpirationMinutes = cacheExpiration;

        if (!string.IsNullOrEmpty(section["DefaultCategory"]))
            options.DefaultCategory = section["DefaultCategory"] ?? string.Empty;

        // Schema from configuration (only if not overridden by parameter)
        if (schema == null && !string.IsNullOrEmpty(section["Schema"]))
            options.Schema = section["Schema"];
    }

    /// <summary>
    /// Adds core dependencies and service registration
    /// </summary>
    /// <typeparam name="TService">Service implementation type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    private static IServiceCollection AddApplicationSettingsCore<TService>(this IServiceCollection services)
        where TService : class, IApplicationSettingsService
    {
        // Add memory cache if not already added
        services.AddMemoryCache();

        // Add the service
        services.AddScoped<IApplicationSettingsService, TService>();

        return services;
    }
}