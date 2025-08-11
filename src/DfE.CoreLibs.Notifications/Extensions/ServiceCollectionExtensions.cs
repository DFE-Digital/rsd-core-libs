using DfE.CoreLibs.Notifications.Interfaces;
using DfE.CoreLibs.Notifications.Options;
using DfE.CoreLibs.Notifications.Providers;
using DfE.CoreLibs.Notifications.Services;
using DfE.CoreLibs.Notifications.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DfE.CoreLibs.Notifications.Extensions;

/// <summary>
/// Extension methods for configuring notification services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add notification services with Redis storage
    /// Reads configuration from appsettings.json under "NotificationService" section
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddNotificationServicesWithRedis(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options from configuration
        services.Configure<NotificationServiceOptions>(configuration.GetSection(NotificationServiceOptions.SectionName));

        // Ensure HttpContextAccessor is registered for SessionUserContextProvider
        services.AddHttpContextAccessor();

        // Configure Redis connection - lazy connection to avoid connection issues during startup
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<NotificationServiceOptions>>().Value;
            var connectionString = options.RedisConnectionString;
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Try to get from configuration if not set in options
                connectionString = configuration.GetConnectionString("Redis") 
                    ?? configuration.GetValue<string>("Redis:ConnectionString")
                    ?? throw new InvalidOperationException("Redis connection string not found in configuration. Please set it in appsettings.json under 'ConnectionStrings:Redis' or 'Redis:ConnectionString' or 'NotificationService:RedisConnectionString'");
            }

            // Use lazy connection with retry options for better reliability
            var configOptions = ConfigurationOptions.Parse(connectionString);
            configOptions.AbortOnConnectFail = false;
            configOptions.ConnectRetry = 3;
            configOptions.ReconnectRetryPolicy = new ExponentialRetry(5000);
            
            return ConnectionMultiplexer.Connect(configOptions);
        });

        // Register core services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserContextProvider, SessionUserContextProvider>();
        services.AddScoped<INotificationStorage, RedisNotificationStorage>();

        return services;
    }

    /// <summary>
    /// Add notification services with session storage
    /// Reads configuration from appsettings.json under "NotificationService" section
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddNotificationServicesWithSession(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options from configuration
        services.Configure<NotificationServiceOptions>(configuration.GetSection(NotificationServiceOptions.SectionName));

        // Ensure HttpContextAccessor is registered for session storage
        services.AddHttpContextAccessor();

        // Register core services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserContextProvider, SessionUserContextProvider>();
        services.AddScoped<INotificationStorage, SessionNotificationStorage>();

        return services;
    }

    /// <summary>
    /// Add notification services with in-memory storage
    /// Reads configuration from appsettings.json under "NotificationService" section
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddNotificationServicesWithInMemory(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options from configuration
        services.Configure<NotificationServiceOptions>(configuration.GetSection(NotificationServiceOptions.SectionName));

        // Ensure HttpContextAccessor is registered for SessionUserContextProvider
        services.AddHttpContextAccessor();

        // Register core services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserContextProvider, SessionUserContextProvider>();
        services.AddSingleton<INotificationStorage, InMemoryNotificationStorage>();

        return services;
    }

    /// <summary>
    /// Add notification services with custom storage and context providers
    /// </summary>
    /// <typeparam name="TStorage">Custom storage implementation</typeparam>
    /// <typeparam name="TContextProvider">Custom context provider implementation</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddNotificationServicesWithCustomProviders<TStorage, TContextProvider>(
        this IServiceCollection services, 
        IConfiguration configuration)
        where TStorage : class, INotificationStorage
        where TContextProvider : class, IUserContextProvider
    {
        // Configure options from configuration
        services.Configure<NotificationServiceOptions>(configuration.GetSection(NotificationServiceOptions.SectionName));

        // Ensure HttpContextAccessor is registered (in case custom providers need it)
        services.AddHttpContextAccessor();

        // Register core services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserContextProvider, TContextProvider>();
        services.AddScoped<INotificationStorage, TStorage>();

        return services;
    }

    /// <summary>
    /// Add notification services with Redis storage using explicit configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="redisConnectionString">Redis connection string</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddNotificationServicesWithRedis(this IServiceCollection services, string redisConnectionString, Action<NotificationServiceOptions>? configureOptions = null)
    {
        if (string.IsNullOrWhiteSpace(redisConnectionString))
            throw new ArgumentException("Redis connection string cannot be null or empty", nameof(redisConnectionString));

        // Ensure HttpContextAccessor is registered for SessionUserContextProvider
        services.AddHttpContextAccessor();

        // Configure Redis connection
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        // Configure options
        services.Configure<NotificationServiceOptions>(options =>
        {
            options.StorageProvider = NotificationStorageProvider.Redis;
            options.RedisConnectionString = redisConnectionString;
            configureOptions?.Invoke(options);
        });

        // Register core services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserContextProvider, SessionUserContextProvider>();
        services.AddScoped<INotificationStorage, RedisNotificationStorage>();

        return services;
    }

    /// <summary>
    /// Add notification services with session storage using explicit configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddNotificationServicesWithSession(this IServiceCollection services, Action<NotificationServiceOptions>? configureOptions = null)
    {
        // Ensure HttpContextAccessor is registered for session storage
        services.AddHttpContextAccessor();

        // Configure options
        services.Configure<NotificationServiceOptions>(options =>
        {
            options.StorageProvider = NotificationStorageProvider.Session;
            configureOptions?.Invoke(options);
        });

        // Register core services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserContextProvider, SessionUserContextProvider>();
        services.AddScoped<INotificationStorage, SessionNotificationStorage>();

        return services;
    }

    /// <summary>
    /// Add notification services with in-memory storage using explicit configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddNotificationServicesWithInMemory(this IServiceCollection services, Action<NotificationServiceOptions>? configureOptions = null)
    {
        // Ensure HttpContextAccessor is registered for SessionUserContextProvider
        services.AddHttpContextAccessor();

        // Configure options
        services.Configure<NotificationServiceOptions>(options =>
        {
            options.StorageProvider = NotificationStorageProvider.InMemory;
            configureOptions?.Invoke(options);
        });

        // Register core services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserContextProvider, SessionUserContextProvider>();
        services.AddSingleton<INotificationStorage, InMemoryNotificationStorage>();

        return services;
    }
}