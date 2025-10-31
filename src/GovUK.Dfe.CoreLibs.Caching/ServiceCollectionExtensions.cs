using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Caching.Services;
using GovUK.Dfe.CoreLibs.Caching.Settings;
using Microsoft.Extensions.Caching.Distributed;
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

        /// <summary>
        /// Adds hybrid caching with both memory and Redis, including automatic session support.
        /// This registers IDistributedCache for ASP.NET Core sessions automatically.
        /// </summary>
        public static IServiceCollection AddHybridCaching(
            this IServiceCollection services, IConfiguration config)
        {
            services.Configure<CacheSettings>(config.GetSection("CacheSettings"));

            // Add Memory Cache for fast local caching
            services.AddMemoryCache();
            services.AddSingleton<ICacheService<IMemoryCacheType>, MemoryCacheService>();

            // Add Redis connection
            services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
            {
                var connectionString = GetRedisConnectionString(config);
                return ConnectionMultiplexer.Connect(connectionString);
            });

            // Register Redis cache service with all interfaces
            services.AddSingleton<RedisCacheService>();

            // Standard caching interface (what 99% of users should use)
            services.AddSingleton<ICacheService<IRedisCacheType>>(sp =>
                sp.GetRequiredService<RedisCacheService>());

            // Advanced Redis operations interface (for pattern matching, raw data)
            services.AddSingleton<IAdvancedRedisCacheService>(sp =>
                sp.GetRequiredService<RedisCacheService>());

            // ASP.NET Core distributed cache for session support (automatic)
            services.AddSingleton<IDistributedCache, DistributedCacheAdapter>();

            return services;
        }
    }
}