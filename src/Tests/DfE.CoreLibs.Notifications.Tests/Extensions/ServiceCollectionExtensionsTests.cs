using DfE.CoreLibs.Notifications.Extensions;
using DfE.CoreLibs.Notifications.Interfaces;
using DfE.CoreLibs.Notifications.Models;
using DfE.CoreLibs.Notifications.Options;
using DfE.CoreLibs.Notifications.Services;
using DfE.CoreLibs.Notifications.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace DfE.CoreLibs.Notifications.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddNotificationServices_WithDefaultConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor(); // Required for SessionNotificationStorage

        // Act
        services.AddNotificationServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        Assert.NotNull(serviceProvider.GetService<INotificationService>());
        Assert.NotNull(serviceProvider.GetService<INotificationStorage>());
        Assert.NotNull(serviceProvider.GetService<IUserContextProvider>());
        
        var options = serviceProvider.GetService<IOptions<NotificationServiceOptions>>();
        Assert.NotNull(options);
        Assert.Equal(NotificationStorageProvider.Session, options.Value.StorageProvider);
    }

    [Fact]
    public void AddNotificationServices_WithConfiguration_ShouldBindOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        var configData = new Dictionary<string, string?>
        {
            ["NotificationService:MaxNotificationsPerUser"] = "100",
            ["NotificationService:StorageProvider"] = "InMemory"
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddNotificationServices(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<NotificationServiceOptions>>();
        
        Assert.Equal(100, options.Value.MaxNotificationsPerUser);
        Assert.Equal(NotificationStorageProvider.InMemory, options.Value.StorageProvider);
    }

    [Fact]
    public void AddNotificationServices_WithOptionsAction_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNotificationServices(options =>
        {
            options.MaxNotificationsPerUser = 25;
            options.StorageProvider = NotificationStorageProvider.Redis;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<NotificationServiceOptions>>();
        
        Assert.Equal(25, options.Value.MaxNotificationsPerUser);
        Assert.Equal(NotificationStorageProvider.Redis, options.Value.StorageProvider);
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
    public void AddNotificationServices_WithDefaultOptions_ShouldUseSessionStorage()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNotificationServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<NotificationServiceOptions>>();
        
        Assert.Equal(NotificationStorageProvider.Session, options.Value.StorageProvider);
    }

    [Fact]
    public void AddNotificationServicesWithCustomProviders_ShouldRegisterCustomImplementations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNotificationServicesWithCustomProviders<TestStorage, TestContextProvider>();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var storage = serviceProvider.GetRequiredService<INotificationStorage>();
        var contextProvider = serviceProvider.GetRequiredService<IUserContextProvider>();
        
        Assert.IsType<TestStorage>(storage);
        Assert.IsType<TestContextProvider>(contextProvider);
    }

    [Fact]
    public void AddNotificationServices_WithUnknownStorageProvider_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotificationServices(options =>
        {
            options.StorageProvider = (NotificationStorageProvider)999; // Invalid enum value
        });

        // Act & Assert
        var serviceProvider = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => 
            serviceProvider.GetRequiredService<INotificationStorage>());
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