using DfE.CoreLibs.Notifications.Interfaces;
using DfE.CoreLibs.Notifications.Extensions;
using DfE.CoreLibs.Notifications.Options;
using DfE.CoreLibs.Notifications.Services;
using DfE.CoreLibs.Notifications.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DfE.CoreLibs.Notifications.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddNotificationServices_WithDefaults_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Required for ILogger
        services.AddHttpContextAccessor(); // Required for session support

        // Act
        services.AddNotificationServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        Assert.NotNull(serviceProvider.GetService<INotificationService>());
        Assert.NotNull(serviceProvider.GetService<IUserContextProvider>());
        Assert.NotNull(serviceProvider.GetService<INotificationStorage>());
        
        // Should be registered as scoped
        var service1 = serviceProvider.GetService<INotificationService>();
        var service2 = serviceProvider.GetService<INotificationService>();
        Assert.Same(service1, service2); // Same instance within scope
    }

    [Fact]
    public void AddNotificationServices_WithConfiguration_ShouldApplyOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["NotificationService:MaxNotificationsPerUser"] = "100",
            ["NotificationService:StorageProvider"] = "InMemory",
            ["NotificationService:SessionKey"] = "CustomSessionKey"
        });
        var configuration = configBuilder.Build();

        // Act
        services.AddNotificationServices(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<NotificationServiceOptions>>();
        
        Assert.NotNull(options);
        Assert.Equal(100, options.Value.MaxNotificationsPerUser);
        Assert.Equal(NotificationStorageProvider.InMemory, options.Value.StorageProvider);
        Assert.Equal("CustomSessionKey", options.Value.SessionKey);
    }

    [Fact]
    public void AddNotificationServices_WithAction_ShouldApplyOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();

        // Act
        services.AddNotificationServices(options =>
        {
            options.MaxNotificationsPerUser = 25;
            options.SessionKey = "TestKey";
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<NotificationServiceOptions>>();
        
        Assert.NotNull(options);
        Assert.Equal(25, options.Value.MaxNotificationsPerUser);
        Assert.Equal("TestKey", options.Value.SessionKey);
    }

    [Fact]
    public void AddNotificationServicesWithInMemory_ShouldConfigureInMemoryStorage()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();

        // Act
        services.AddNotificationServicesWithInMemory(options =>
        {
            options.MaxNotificationsPerUser = 10;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<NotificationServiceOptions>>();
        var storage = serviceProvider.GetService<INotificationStorage>();
        
        Assert.NotNull(options);
        Assert.Equal(NotificationStorageProvider.InMemory, options.Value.StorageProvider);
        Assert.Equal(10, options.Value.MaxNotificationsPerUser);
        Assert.IsType<InMemoryNotificationStorage>(storage);
    }

    [Fact]
    public void AddNotificationServicesWithRedis_WithNullConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            services.AddNotificationServicesWithRedis(null!));
    }

    [Fact]
    public void AddNotificationServicesWithRedis_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            services.AddNotificationServicesWithRedis(""));
    }

    [Fact]
    public void AddNotificationServicesWithCustomProviders_ShouldRegisterCustomTypes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNotificationServicesWithCustomProviders<TestStorage, TestContextProvider>();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var storage = serviceProvider.GetService<INotificationStorage>();
        var contextProvider = serviceProvider.GetService<IUserContextProvider>();
        
        Assert.IsType<TestStorage>(storage);
        Assert.IsType<TestContextProvider>(contextProvider);
    }

    [Fact]
    public void AddNotificationServices_ShouldRegisterCorrectServiceTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Required for ILogger
        services.AddHttpContextAccessor();

        // Act
        services.AddNotificationServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Verify concrete types
        var notificationService = serviceProvider.GetService<INotificationService>();
        Assert.IsType<NotificationService>(notificationService);
        
        var storage = serviceProvider.GetService<INotificationStorage>();
        Assert.IsType<SessionNotificationStorage>(storage);
    }

    // Test helper classes
    private class TestStorage : INotificationStorage
    {
        public Task StoreNotificationAsync(DfE.CoreLibs.Notifications.Models.Notification notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IEnumerable<DfE.CoreLibs.Notifications.Models.Notification>> GetNotificationsAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<DfE.CoreLibs.Notifications.Models.Notification>());

        public Task UpdateNotificationAsync(DfE.CoreLibs.Notifications.Models.Notification notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveNotificationAsync(string notificationId, string userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveNotificationsByContextAsync(string context, string userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveNotificationsByCategoryAsync(string category, string userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ClearAllNotificationsAsync(string userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<DfE.CoreLibs.Notifications.Models.Notification?> GetNotificationAsync(string notificationId, string userId, CancellationToken cancellationToken = default)
            => Task.FromResult<DfE.CoreLibs.Notifications.Models.Notification?>(null);
    }

    private class TestContextProvider : IUserContextProvider
    {
        public string GetCurrentUserId() => "test-user";
        public bool IsContextAvailable() => true;
    }
}