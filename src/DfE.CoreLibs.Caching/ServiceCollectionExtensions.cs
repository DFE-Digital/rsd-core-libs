using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Caching.Services;
using DfE.CoreLibs.Caching.Settings;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        private static string GetRedisConnectionString(IConfiguration config)
        {
            // First, check ConnectionStrings:Redis
            var connectionString = config.GetConnectionString("Redis");
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                return connectionString;
            }

            // Fall back to CacheSettings:Redis:ConnectionString
            var cacheSettings = config.GetSection("CacheSettings:Redis").Get<RedisCacheSettings>();
            if (!string.IsNullOrEmpty(cacheSettings?.ConnectionString))
            {
                return cacheSettings.ConnectionString;
            }

            throw new InvalidOperationException(
                "Redis connection string is required but not configured. " +
                "Please configure either 'ConnectionStrings:Redis' or 'CacheSettings:Redis:ConnectionString'");
        }
        public static IServiceCollection AddServiceCaching(
            this IServiceCollection services, IConfiguration config)
        {
            services.Configure<CacheSettings>(config.GetSection("CacheSettings"));
            services.AddMemoryCache();
            services.AddSingleton<ICacheService<IMemoryCacheType>, MemoryCacheService>();

            return services;
        }

        public static IServiceCollection AddRedisCaching(
            this IServiceCollection services, IConfiguration config)
        {
            services.Configure<CacheSettings>(config.GetSection("CacheSettings"));
            
            // Configure Redis connection
            services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
            {
                var connectionString = GetRedisConnectionString(config);
                return ConnectionMultiplexer.Connect(connectionString);
            });

            services.AddSingleton<ICacheService<IRedisCacheType>, RedisCacheService>();

            return services;
        }

        public static IServiceCollection AddHybridCaching(
            this IServiceCollection services, IConfiguration config)
        {
            services.Configure<CacheSettings>(config.GetSection("CacheSettings"));
            
            // Add Memory Cache
            services.AddMemoryCache();
            services.AddSingleton<ICacheService<IMemoryCacheType>, MemoryCacheService>();

            // Add Redis Cache
            services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
            {
                var connectionString = GetRedisConnectionString(config);
                return ConnectionMultiplexer.Connect(connectionString);
            });
            services.AddSingleton<ICacheService<IRedisCacheType>, RedisCacheService>();

            return services;
        }
    }
}