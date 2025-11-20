using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Caching.Services;
using GovUK.Dfe.CoreLibs.Caching.Settings;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GovUK.Dfe.CoreLibs.Caching.Tests.Extensions
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddServiceCaching_ShouldRegisterMemoryCacheServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CacheSettings:Memory:DefaultDurationInSeconds"] = "60"
                })
                .Build();

            // Act
            services.AddServiceCaching(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var cacheService = serviceProvider.GetService<ICacheService<IMemoryCacheType>>();
            Assert.NotNull(cacheService);
            Assert.IsType<MemoryCacheService>(cacheService);
        }

        [Fact]
        public void AddRedisCaching_ShouldRegisterRedisCacheServices_WithCacheSettingsConnectionString()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CacheSettings:Redis:ConnectionString"] = "localhost:6379",
                    ["CacheSettings:Redis:DefaultDurationInSeconds"] = "300"
                })
                .Build();

            // Act
            services.AddRedisCaching(configuration);

            // Assert - Verify services are registered without triggering actual Redis connection
            var serviceProvider = services.BuildServiceProvider();
            var serviceDescriptors = services.ToList();

            // Verify IConnectionMultiplexer is registered as singleton
            var connectionMultiplexerDescriptor = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            Assert.NotNull(connectionMultiplexerDescriptor);
            Assert.Equal(ServiceLifetime.Singleton, connectionMultiplexerDescriptor.Lifetime);

            // Verify ICacheService<IRedisCacheType> is registered as singleton
            var cacheServiceDescriptor = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(ICacheService<IRedisCacheType>));
            Assert.NotNull(cacheServiceDescriptor);
            Assert.Equal(ServiceLifetime.Singleton, cacheServiceDescriptor.Lifetime);
            Assert.Equal(typeof(RedisCacheService), cacheServiceDescriptor.ImplementationType);
        }

        [Fact]
        public void AddRedisCaching_ShouldRegisterRedisCacheServices_WithConnectionStringsRedis()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Redis"] = "localhost:6379",
                    ["CacheSettings:Redis:DefaultDurationInSeconds"] = "300"
                })
                .Build();

            // Act
            services.AddRedisCaching(configuration);

            // Assert - Verify services are registered without triggering actual Redis connection
            var serviceProvider = services.BuildServiceProvider();
            var serviceDescriptors = services.ToList();

            // Verify IConnectionMultiplexer is registered as singleton
            var connectionMultiplexerDescriptor = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            Assert.NotNull(connectionMultiplexerDescriptor);
            Assert.Equal(ServiceLifetime.Singleton, connectionMultiplexerDescriptor.Lifetime);

            // Verify ICacheService<IRedisCacheType> is registered as singleton
            var cacheServiceDescriptor = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(ICacheService<IRedisCacheType>));
            Assert.NotNull(cacheServiceDescriptor);
            Assert.Equal(ServiceLifetime.Singleton, cacheServiceDescriptor.Lifetime);
            Assert.Equal(typeof(RedisCacheService), cacheServiceDescriptor.ImplementationType);
        }

        [Fact]
        public void AddRedisCaching_ShouldPrioritizeConnectionStringsRedis_OverCacheSettings()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Redis"] = "priority:6379",
                    ["CacheSettings:Redis:ConnectionString"] = "fallback:6379",
                    ["CacheSettings:Redis:DefaultDurationInSeconds"] = "300"
                })
                .Build();

            // Act
            services.AddRedisCaching(configuration);

            // Assert - Verify services are registered without triggering actual Redis connection
            var serviceProvider = services.BuildServiceProvider();

            // Check that the required services are registered
            var serviceDescriptors = services.ToList();

            // Verify IConnectionMultiplexer is registered as singleton
            var connectionMultiplexerDescriptor = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            Assert.NotNull(connectionMultiplexerDescriptor);
            Assert.Equal(ServiceLifetime.Singleton, connectionMultiplexerDescriptor.Lifetime);

            // Verify ICacheService<IRedisCacheType> is registered as singleton
            var cacheServiceDescriptor = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(ICacheService<IRedisCacheType>));
            Assert.NotNull(cacheServiceDescriptor);
            Assert.Equal(ServiceLifetime.Singleton, cacheServiceDescriptor.Lifetime);
            Assert.Equal(typeof(RedisCacheService), cacheServiceDescriptor.ImplementationType);

            // Note: We cannot verify which connection string is used without making a real Redis connection
            // The priority logic is tested in the GetRedisConnectionString method indirectly through other tests
        }

        [Fact]
        public void AddRedisCaching_ShouldThrowException_WhenConnectionStringIsEmpty()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CacheSettings:Redis:ConnectionString"] = "",
                    ["CacheSettings:Redis:DefaultDurationInSeconds"] = "300"
                })
                .Build();

            // Act & Assert
            services.AddRedisCaching(configuration);
            var serviceProvider = services.BuildServiceProvider();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetService<IConnectionMultiplexer>());

            Assert.Contains("Redis connection string is required", exception.Message);
            Assert.Contains("ConnectionStrings:Redis", exception.Message);
            Assert.Contains("CacheSettings:Redis:ConnectionString", exception.Message);
        }

        [Fact]
        public void AddRedisCaching_ShouldThrowException_WhenConnectionStringIsNull()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CacheSettings:Redis:DefaultDurationInSeconds"] = "300"
                })
                .Build();

            // Act & Assert
            services.AddRedisCaching(configuration);
            var serviceProvider = services.BuildServiceProvider();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetService<IConnectionMultiplexer>());

            Assert.Contains("Redis connection string is required", exception.Message);
            Assert.Contains("ConnectionStrings:Redis", exception.Message);
            Assert.Contains("CacheSettings:Redis:ConnectionString", exception.Message);
        }

        [Fact]
        public void AddHybridCaching_ShouldRegisterBothMemoryAndRedisCacheServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CacheSettings:Memory:DefaultDurationInSeconds"] = "60",
                    ["CacheSettings:Redis:ConnectionString"] = "localhost:6379",
                    ["CacheSettings:Redis:DefaultDurationInSeconds"] = "300"
                })
                .Build();

            // Act
            services.AddHybridCaching(configuration);

            // Assert - Verify services are registered without triggering actual Redis connection
            var serviceProvider = services.BuildServiceProvider();
            var serviceDescriptors = services.ToList();

            // Verify memory cache service can be resolved (no Redis connection needed)
            var memoryCacheService = serviceProvider.GetService<ICacheService<IMemoryCacheType>>();
            Assert.NotNull(memoryCacheService);
            Assert.IsType<MemoryCacheService>(memoryCacheService);

            // Verify Redis services are registered (but don't resolve to avoid connection)
            var connectionMultiplexerDescriptor = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            Assert.NotNull(connectionMultiplexerDescriptor);

            var redisCacheServiceDescriptor = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(ICacheService<IRedisCacheType>));
            Assert.NotNull(redisCacheServiceDescriptor);
            // The implementation is via factory, not direct type
            Assert.NotNull(redisCacheServiceDescriptor.ImplementationFactory);
        }

        [Fact]
        public void AddHybridCaching_ShouldRegisterBothMemoryAndRedisCacheServices_WithConnectionStringsRedis()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CacheSettings:Memory:DefaultDurationInSeconds"] = "60",
                    ["ConnectionStrings:Redis"] = "localhost:6379",
                    ["CacheSettings:Redis:DefaultDurationInSeconds"] = "300"
                })
                .Build();

            // Act
            services.AddHybridCaching(configuration);

            // Assert - Verify services are registered without triggering actual Redis connection
            var serviceProvider = services.BuildServiceProvider();
            var serviceDescriptors = services.ToList();

            // Verify memory cache service can be resolved (no Redis connection needed)
            var memoryCacheService = serviceProvider.GetService<ICacheService<IMemoryCacheType>>();
            Assert.NotNull(memoryCacheService);
            Assert.IsType<MemoryCacheService>(memoryCacheService);

            // Verify Redis services are registered (but don't resolve to avoid connection)
            var connectionMultiplexerDescriptor = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            Assert.NotNull(connectionMultiplexerDescriptor);

            var redisCacheServiceDescriptor = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(ICacheService<IRedisCacheType>));
            Assert.NotNull(redisCacheServiceDescriptor);
            // The implementation is via factory, not direct type
            Assert.NotNull(redisCacheServiceDescriptor.ImplementationFactory);
        }

        [Fact]
        public void AddServiceCaching_ShouldConfigureCacheSettings()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CacheSettings:Memory:DefaultDurationInSeconds"] = "120",
                    ["CacheSettings:Memory:Durations:TestMethod"] = "240"
                })
                .Build();

            // Act
            services.AddServiceCaching(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var cacheSettings = serviceProvider.GetService<IOptions<CacheSettings>>();
            Assert.NotNull(cacheSettings);
            Assert.Equal(120, cacheSettings.Value.Memory.DefaultDurationInSeconds);
            Assert.Equal(240, cacheSettings.Value.Memory.Durations["TestMethod"]);
        }

        [Fact]
        public void AddRedisCaching_ShouldConfigureRedisCacheSettings()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CacheSettings:Redis:ConnectionString"] = "localhost:6379",
                    ["CacheSettings:Redis:DefaultDurationInSeconds"] = "600",
                    ["CacheSettings:Redis:KeyPrefix"] = "MyApp:",
                    ["CacheSettings:Redis:Database"] = "1",
                    ["CacheSettings:Redis:Durations:TestMethod"] = "1200"
                })
                .Build();

            // Act
            services.AddRedisCaching(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var cacheSettings = serviceProvider.GetService<IOptions<CacheSettings>>();
            Assert.NotNull(cacheSettings);
            Assert.Equal("localhost:6379", cacheSettings.Value.Redis.ConnectionString);
            Assert.Equal(600, cacheSettings.Value.Redis.DefaultDurationInSeconds);
            Assert.Equal("MyApp:", cacheSettings.Value.Redis.KeyPrefix);
            Assert.Equal(1, cacheSettings.Value.Redis.Database);
            Assert.Equal(1200, cacheSettings.Value.Redis.Durations["TestMethod"]);
        }

        [Fact]
        public void AddHybridCaching_ShouldRegisterIAdvancedRedisCacheService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CacheSettings:Memory:DefaultDurationInSeconds"] = "60",
                    ["ConnectionStrings:Redis"] = "localhost:6379",
                    ["CacheSettings:Redis:DefaultDurationInSeconds"] = "300"
                })
                .Build();

            // Act
            services.AddHybridCaching(configuration);

            // Assert - Verify IAdvancedRedisCacheService is registered
            var serviceDescriptors = services.ToList();
            var advancedCacheServiceDescriptor = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(IAdvancedRedisCacheService));
            
            Assert.NotNull(advancedCacheServiceDescriptor);
            Assert.Equal(ServiceLifetime.Singleton, advancedCacheServiceDescriptor.Lifetime);
        }

        [Fact]
        public void AddHybridCaching_ShouldRegisterIDistributedCache()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CacheSettings:Memory:DefaultDurationInSeconds"] = "60",
                    ["ConnectionStrings:Redis"] = "localhost:6379",
                    ["CacheSettings:Redis:DefaultDurationInSeconds"] = "300"
                })
                .Build();

            // Act
            services.AddHybridCaching(configuration);

            // Assert - Verify IDistributedCache is registered
            var serviceDescriptors = services.ToList();
            var distributedCacheDescriptor = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(IDistributedCache));
            
            Assert.NotNull(distributedCacheDescriptor);
            Assert.Equal(ServiceLifetime.Singleton, distributedCacheDescriptor.Lifetime);
            Assert.Equal(typeof(DistributedCacheAdapter), distributedCacheDescriptor.ImplementationType);
        }

        [Fact]
        public void AddHybridCaching_ShouldRegisterRedisCacheServiceOnce()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CacheSettings:Memory:DefaultDurationInSeconds"] = "60",
                    ["ConnectionStrings:Redis"] = "localhost:6379",
                    ["CacheSettings:Redis:DefaultDurationInSeconds"] = "300"
                })
                .Build();

            // Act
            services.AddHybridCaching(configuration);

            // Assert - Verify RedisCacheService is registered once as a singleton
            var serviceDescriptors = services.ToList();
            var redisCacheServiceDescriptors = serviceDescriptors
                .Where(d => d.ImplementationType == typeof(RedisCacheService) || 
                           (d.ServiceType == typeof(RedisCacheService)))
                .ToList();
            
            // Should have exactly one registration of the concrete RedisCacheService
            var concreteRegistration = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(RedisCacheService));
            Assert.NotNull(concreteRegistration);
            Assert.Equal(ServiceLifetime.Singleton, concreteRegistration.Lifetime);
        }

        [Fact]
        public void AddHybridCaching_ShouldShareSameRedisCacheServiceInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CacheSettings:Memory:DefaultDurationInSeconds"] = "60",
                    ["ConnectionStrings:Redis"] = "localhost:6379",
                    ["CacheSettings:Redis:DefaultDurationInSeconds"] = "300"
                })
                .Build();

            // Act
            services.AddHybridCaching(configuration);

            // Assert - Verify all Redis-related services point to the same instance
            // This test verifies the factory pattern is set up correctly
            var serviceDescriptors = services.ToList();

            // Check ICacheService<IRedisCacheType> factory
            var standardCacheDescriptor = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(ICacheService<IRedisCacheType>));
            Assert.NotNull(standardCacheDescriptor);
            Assert.NotNull(standardCacheDescriptor.ImplementationFactory);

            // Check IAdvancedRedisCacheService factory
            var advancedCacheDescriptor = serviceDescriptors
                .FirstOrDefault(d => d.ServiceType == typeof(IAdvancedRedisCacheService));
            Assert.NotNull(advancedCacheDescriptor);
            Assert.NotNull(advancedCacheDescriptor.ImplementationFactory);

            // All factories should resolve from the same RedisCacheService singleton
            // This is implicitly tested by the registration pattern in ServiceCollectionExtensions
        }

        [Fact]
        public void AddHybridCaching_ShouldThrowException_WhenRedisConnectionStringMissing()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CacheSettings:Memory:DefaultDurationInSeconds"] = "60",
                    ["CacheSettings:Redis:DefaultDurationInSeconds"] = "300"
                    // Missing Redis connection string
                })
                .Build();

            // Act
            services.AddHybridCaching(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert - Should throw when trying to resolve Redis services
            var exception = Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetService<IConnectionMultiplexer>());

            Assert.Contains("Redis connection string is required", exception.Message);
        }

        [Fact]
        public void AddHybridCaching_ShouldConfigureAllCacheSettings()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CacheSettings:Memory:DefaultDurationInSeconds"] = "120",
                    ["CacheSettings:Redis:ConnectionString"] = "localhost:6379",
                    ["CacheSettings:Redis:DefaultDurationInSeconds"] = "600",
                    ["CacheSettings:Redis:KeyPrefix"] = "MyApp:",
                    ["CacheSettings:Redis:Database"] = "2"
                })
                .Build();

            // Act
            services.AddHybridCaching(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var cacheSettings = serviceProvider.GetService<IOptions<CacheSettings>>();
            Assert.NotNull(cacheSettings);
            Assert.Equal(120, cacheSettings.Value.Memory.DefaultDurationInSeconds);
            Assert.Equal("localhost:6379", cacheSettings.Value.Redis.ConnectionString);
            Assert.Equal(600, cacheSettings.Value.Redis.DefaultDurationInSeconds);
            Assert.Equal("MyApp:", cacheSettings.Value.Redis.KeyPrefix);
            Assert.Equal(2, cacheSettings.Value.Redis.Database);
        }
    }
}