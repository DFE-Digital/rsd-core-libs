using DfE.CoreLibs.Notifications.Extensions;
using DfE.CoreLibs.Notifications.Interfaces;
using DfE.CoreLibs.Notifications.Models;
using DfE.CoreLibs.Notifications.Options;
using DfE.CoreLibs.Notifications.Services;
using DfE.CoreLibs.Notifications.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace DfE.CoreLibs.Notifications.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddNotificationServicesWithRedis_WithConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Redis"] = "localhost:6379",
            ["NotificationService:MaxNotificationsPerUser"] = "100",
            ["NotificationService:StorageProvider"] = "Redis"
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddNotificationServicesWithRedis(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Only test IUserContextProvider as it doesn't depend on Redis
        Assert.NotNull(serviceProvider.GetService<IUserContextProvider>());
        
        // Don't resolve INotificationService, INotificationStorage, or IConnectionMultiplexer as they would try to connect to Redis
        // Assert.NotNull(serviceProvider.GetService<INotificationService>());
        // Assert.NotNull(serviceProvider.GetService<INotificationStorage>());
        // Assert.NotNull(serviceProvider.GetService<IConnectionMultiplexer>());
        
        var options = serviceProvider.GetService<IOptions<NotificationServiceOptions>>();
        Assert.NotNull(options);
        Assert.Equal(100, options.Value.MaxNotificationsPerUser);
    }

    [Fact]
    public void AddNotificationServicesWithSession_WithConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        var configData = new Dictionary<string, string?>
        {
            ["NotificationService:MaxNotificationsPerUser"] = "50",
            ["NotificationService:StorageProvider"] = "Session"
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddNotificationServicesWithSession(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        Assert.NotNull(serviceProvider.GetService<INotificationService>());
        Assert.NotNull(serviceProvider.GetService<INotificationStorage>());
        Assert.NotNull(serviceProvider.GetService<IUserContextProvider>());
        Assert.NotNull(serviceProvider.GetService<IHttpContextAccessor>());
        
        var options = serviceProvider.GetService<IOptions<NotificationServiceOptions>>();
        Assert.NotNull(options);
        Assert.Equal(50, options.Value.MaxNotificationsPerUser);
    }

    [Fact]
    public void AddNotificationServicesWithInMemory_WithConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        var configData = new Dictionary<string, string?>
        {
            ["NotificationService:MaxNotificationsPerUser"] = "25",
            ["NotificationService:StorageProvider"] = "InMemory"
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddNotificationServicesWithInMemory(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        Assert.NotNull(serviceProvider.GetService<INotificationService>());
        Assert.NotNull(serviceProvider.GetService<INotificationStorage>());
        Assert.NotNull(serviceProvider.GetService<IUserContextProvider>());
        
        var options = serviceProvider.GetService<IOptions<NotificationServiceOptions>>();
        Assert.NotNull(options);
        Assert.Equal(25, options.Value.MaxNotificationsPerUser);
    }

    [Fact]
    public void AddNotificationServicesWithSession_WithOptionsAction_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNotificationServicesWithSession(options =>
        {
            options.MaxNotificationsPerUser = 25;
            options.StorageProvider = NotificationStorageProvider.Session;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<NotificationServiceOptions>>();
        
        Assert.Equal(25, options.Value.MaxNotificationsPerUser);
        Assert.Equal(NotificationStorageProvider.Session, options.Value.StorageProvider);
    }

    [Fact]
    public void AddNotificationServicesWithInMemory_ShouldRegisterInMemoryStorage()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNotificationServicesWithInMemory();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var storage = serviceProvider.GetRequiredService<INotificationStorage>();
        var options = serviceProvider.GetRequiredService<IOptions<NotificationServiceOptions>>();
        
        Assert.IsType<InMemoryNotificationStorage>(storage);
        Assert.Equal(NotificationStorageProvider.InMemory, options.Value.StorageProvider);
    }

    [Fact]
    public void AddNotificationServicesWithInMemory_WithOptionsAction_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNotificationServicesWithInMemory(options =>
        {
            options.MaxNotificationsPerUser = 200;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<NotificationServiceOptions>>();
        
        Assert.Equal(200, options.Value.MaxNotificationsPerUser);
        Assert.Equal(NotificationStorageProvider.InMemory, options.Value.StorageProvider);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddNotificationServicesWithRedis_WithInvalidConnectionString_ShouldThrowArgumentException(string? connectionString)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            services.AddNotificationServicesWithRedis(connectionString!));
    }

    [Fact]
    public void AddNotificationServicesWithRedis_WithValidConnectionString_ShouldRegisterRedisStorage()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNotificationServicesWithRedis("localhost:6379");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<NotificationServiceOptions>>();
        
        Assert.Equal(NotificationStorageProvider.Redis, options.Value.StorageProvider);
        Assert.Equal("localhost:6379", options.Value.RedisConnectionString);
        
        // Only test IUserContextProvider as it doesn't depend on Redis
        Assert.NotNull(serviceProvider.GetService<IUserContextProvider>());
        
        // Don't resolve INotificationService or INotificationStorage as they would try to connect to Redis
        // Assert.NotNull(serviceProvider.GetService<INotificationService>());
        // Assert.NotNull(serviceProvider.GetService<INotificationStorage>());
    }

    [Fact]
    public void AddNotificationServicesWithRedis_WithOptionsAction_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNotificationServicesWithRedis("localhost:6379", options =>
        {
            options.MaxNotificationsPerUser = 150;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<NotificationServiceOptions>>();
        
        Assert.Equal(150, options.Value.MaxNotificationsPerUser);
        Assert.Equal("localhost:6379", options.Value.RedisConnectionString);
    }

    [Fact]
    public void AddNotificationServicesWithCustomProviders_ShouldRegisterCustomImplementations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        var configData = new Dictionary<string, string?>
        {
            ["NotificationService:MaxNotificationsPerUser"] = "50"
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddNotificationServicesWithCustomProviders<TestStorage, TestContextProvider>(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var storage = serviceProvider.GetRequiredService<INotificationStorage>();
        var contextProvider = serviceProvider.GetRequiredService<IUserContextProvider>();
        
        Assert.IsType<TestStorage>(storage);
        Assert.IsType<TestContextProvider>(contextProvider);
    }

    [Fact]
    public void AddNotificationServicesWithRedis_WithMissingConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        var configData = new Dictionary<string, string?>
        {
            ["NotificationService:MaxNotificationsPerUser"] = "50"
            // No Redis connection string
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act & Assert - Exception should be thrown during service registration
        // because the configuration doesn't contain a Redis connection string
        var exception = Assert.Throws<InvalidOperationException>(() => 
            services.AddNotificationServicesWithRedis(configuration));
        
        Assert.Contains("Redis connection string not found", exception.Message);
    }

    [Fact]
    public void AddNotificationServicesWithRedis_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNotificationServicesWithRedis("localhost:6379");

        // Assert - Verify that all services are registered without resolving them
        var serviceProvider = services.BuildServiceProvider();
        
        // Check that the service descriptors exist without resolving the services
        var serviceDescriptors = services.ToList();
        
        Assert.Contains(serviceDescriptors, sd => sd.ServiceType == typeof(INotificationService));
        Assert.Contains(serviceDescriptors, sd => sd.ServiceType == typeof(INotificationStorage));
        Assert.Contains(serviceDescriptors, sd => sd.ServiceType == typeof(IUserContextProvider));
        Assert.Contains(serviceDescriptors, sd => sd.ServiceType == typeof(IConnectionMultiplexer));
        
        // Verify options are configured correctly
        var options = serviceProvider.GetRequiredService<IOptions<NotificationServiceOptions>>();
        Assert.Equal(NotificationStorageProvider.Redis, options.Value.StorageProvider);
        Assert.Equal("localhost:6379", options.Value.RedisConnectionString);
    }

    // Test implementations
    private class TestStorage : INotificationStorage
    {
        public Task StoreNotificationAsync(Notification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Notification?> GetNotificationAsync(string id, string userId, CancellationToken cancellationToken = default) => Task.FromResult<Notification?>(null);
        public Task<IEnumerable<Notification>> GetNotificationsAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<Notification>());
        public Task UpdateNotificationAsync(Notification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveNotificationAsync(string id, string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveNotificationsByContextAsync(string context, string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveNotificationsByCategoryAsync(string category, string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ClearAllNotificationsAsync(string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class TestContextProvider : IUserContextProvider
    {
        public string GetCurrentUserId() => "test-user";
        public bool IsContextAvailable() => true;
    }
}