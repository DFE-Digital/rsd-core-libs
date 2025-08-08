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
    /// Add notification services with configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddNotificationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<NotificationServiceOptions>(configuration.GetSection(NotificationServiceOptions.SectionName));

        // Register core services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserContextProvider, SessionUserContextProvider>();

        // Register storage based on configuration
        services.AddScoped<INotificationStorage>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<NotificationServiceOptions>>().Value;
            
            return options.StorageProvider switch
            {
                NotificationStorageProvider.Session => new SessionNotificationStorage(
                    serviceProvider.GetRequiredService<IHttpContextAccessor>(),
                    serviceProvider.GetRequiredService<IOptions<NotificationServiceOptions>>()),
                
                NotificationStorageProvider.Redis => new RedisNotificationStorage(
                    serviceProvider.GetRequiredService<IConnectionMultiplexer>(),
                    serviceProvider.GetRequiredService<IOptions<NotificationServiceOptions>>()),
                
                NotificationStorageProvider.InMemory => new InMemoryNotificationStorage(
                    serviceProvider.GetRequiredService<IOptions<NotificationServiceOptions>>()),
                
                _ => throw new InvalidOperationException($"Unsupported storage provider: {options.StorageProvider}")
            };
        });

        return services;
    }

    /// <summary>
    /// Add notification services with session storage
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddNotificationServices(this IServiceCollection services, Action<NotificationServiceOptions>? configureOptions = null)
    {
        
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<NotificationServiceOptions>(options => { });
        }

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserContextProvider, SessionUserContextProvider>();
        services.AddScoped<INotificationStorage, SessionNotificationStorage>();

        return services;
    }

    /// <summary>
    /// Add notification services with Redis storage
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="redisConnectionString">Redis connection string</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddNotificationServicesWithRedis(this IServiceCollection services, string redisConnectionString, Action<NotificationServiceOptions>? configureOptions = null)
    {
        if (string.IsNullOrWhiteSpace(redisConnectionString))
            throw new ArgumentException("Redis connection string cannot be null or empty", nameof(redisConnectionString));

        // Configure Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        // Configure options
        services.Configure<NotificationServiceOptions>(options =>
        {
            options.StorageProvider = NotificationStorageProvider.Redis;
            options.RedisConnectionString = redisConnectionString;
            configureOptions?.Invoke(options);
        });



        // Register services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserContextProvider, SessionUserContextProvider>();
        services.AddScoped<INotificationStorage, RedisNotificationStorage>();

        return services;
    }

    /// <summary>
    /// Add notification services with in-memory storage
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddNotificationServicesWithInMemory(this IServiceCollection services, Action<NotificationServiceOptions>? configureOptions = null)
    {
        services.Configure<NotificationServiceOptions>(options =>
        {
            options.StorageProvider = NotificationStorageProvider.InMemory;
            configureOptions?.Invoke(options);
        });


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
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddNotificationServicesWithCustomProviders<TStorage, TContextProvider>(
        this IServiceCollection services, 
        Action<NotificationServiceOptions>? configureOptions = null)
        where TStorage : class, INotificationStorage
        where TContextProvider : class, IUserContextProvider
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<NotificationServiceOptions>(options => { });
        }

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserContextProvider, TContextProvider>();
        services.AddScoped<INotificationStorage, TStorage>();

        return services;
    }
}