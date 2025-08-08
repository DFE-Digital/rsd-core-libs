using DfE.CoreLibs.Notifications.Contracts.Models;
using DfE.CoreLibs.Notifications.Options;
using DfE.CoreLibs.Notifications.Storage;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;
using System.Text.Json;

namespace DfE.CoreLibs.Notifications.Tests.Storage;

public class RedisNotificationStorageTests
{
    private readonly IConnectionMultiplexer _mockConnectionMultiplexer;
    private readonly IDatabase _mockDatabase;
    private readonly NotificationServiceOptions _options;
    private readonly RedisNotificationStorage _storage;
    private readonly Dictionary<string, RedisValue> _redisStorage;

    public RedisNotificationStorageTests()
    {
        _mockConnectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        _mockDatabase = Substitute.For<IDatabase>();
        _redisStorage = new Dictionary<string, RedisValue>();
        
        _options = new NotificationServiceOptions 
        { 
            MaxNotificationsPerUser = 3,
            RedisKeyPrefix = "test_notifications:",
            MaxNotificationAgeHours = 24
        };
        
        var mockOptions = Microsoft.Extensions.Options.Options.Create(_options);
        
        // Setup mock database to use our dictionary
        _mockDatabase.StringGetAsync(Arg.Any<RedisKey>()).Returns(info =>
        {
            var key = info.Arg<RedisKey>().ToString();
            return _redisStorage.TryGetValue(key, out var value) ? value : RedisValue.Null;
        });

        _mockDatabase.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>()).Returns(info =>
        {
            var key = info.Arg<RedisKey>().ToString();
            var value = info.Arg<RedisValue>();
            _redisStorage[key] = value;
            return Task.FromResult(true);
        });

        _mockDatabase.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>()).Returns(info =>
        {
            var key = info.Arg<RedisKey>().ToString();
            var value = info.Arg<RedisValue>();
            _redisStorage[key] = value;
            return Task.FromResult(true);
        });

        _mockDatabase.KeyDeleteAsync(Arg.Any<RedisKey>()).Returns(info =>
        {
            var key = info.Arg<RedisKey>().ToString();
            var removed = _redisStorage.Remove(key);
            return Task.FromResult(removed);
        });

        _mockDatabase.KeyExpireAsync(Arg.Any<RedisKey>(), Arg.Any<TimeSpan>()).Returns(Task.FromResult(true));

        _mockConnectionMultiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(_mockDatabase);

        _storage = new RedisNotificationStorage(_mockConnectionMultiplexer, mockOptions);
    }

    [Fact]
    public async Task StoreNotificationAsync_ShouldStoreNotificationInRedis()
    {
        // Arrange
        var notification = new Notification
        {
            Id = "test-1",
            Message = "Test message",
            UserId = "user-1"
        };

        // Act
        await _storage.StoreNotificationAsync(notification);

        // Assert
        var expectedKey = "test_notifications:user-1";
        await _mockDatabase.Received(1).StringSetAsync(expectedKey, Arg.Any<RedisValue>());
        
        var storedJson = _redisStorage[expectedKey];
        var storedNotifications = JsonSerializer.Deserialize<List<Notification>>(storedJson!);
        
        Assert.Single(storedNotifications);
        Assert.Equal("test-1", storedNotifications[0].Id);
        Assert.Equal("Test message", storedNotifications[0].Message);
    }

    [Fact]
    public async Task StoreNotificationAsync_WithDefaultUserId_ShouldUseDefaultKey()
    {
        // Arrange
        var notification = new Notification
        {
            Id = "test-1",
            Message = "Test message",
            UserId = null // This should default to "default"
        };

        // Act
        await _storage.StoreNotificationAsync(notification);

        // Assert
        var expectedKey = "test_notifications:default";
        await _mockDatabase.Received(1).StringSetAsync(expectedKey, Arg.Any<RedisValue>());
    }

    [Fact]
    public async Task StoreNotificationAsync_WithSameContext_ShouldReplaceExisting()
    {
        // Arrange
        var notification1 = new Notification
        {
            Id = "test-1",
            Message = "First message",
            Context = "same-context",
            UserId = "user-1"
        };

        var notification2 = new Notification
        {
            Id = "test-2",
            Message = "Second message",
            Context = "same-context",
            UserId = "user-1"
        };

        // Act
        await _storage.StoreNotificationAsync(notification1);
        await _storage.StoreNotificationAsync(notification2);

        // Assert
        var notifications = await _storage.GetNotificationsAsync("user-1");
        Assert.Single(notifications);
        Assert.Equal("test-2", notifications.First().Id);
        Assert.Equal("Second message", notifications.First().Message);
    }

    [Fact]
    public async Task StoreNotificationAsync_ExceedsMaxLimit_ShouldTrimOldNotifications()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        
        for (int i = 1; i <= 5; i++)
        {
            var notification = new Notification
            {
                Id = $"test-{i}",
                Message = $"Message {i}",
                UserId = "user-1",
                CreatedAt = baseTime.AddMinutes(i)
            };
            await _storage.StoreNotificationAsync(notification);
        }

        // Act
        var notifications = await _storage.GetNotificationsAsync("user-1");

        // Assert
        Assert.Equal(3, notifications.Count()); // Should respect MaxNotificationsPerUser
        
        // Should keep the most recent ones (3, 4, 5)
        var notificationIds = notifications.Select(n => n.Id).ToList();
        Assert.Contains("test-3", notificationIds);
        Assert.Contains("test-4", notificationIds);
        Assert.Contains("test-5", notificationIds);
    }

    [Fact]
    public async Task StoreNotificationAsync_WithMaxAgeHours_ShouldSetExpiration()
    {
        // Arrange
        var notification = new Notification
        {
            Id = "test-1",
            UserId = "user-1"
        };

        // Act
        await _storage.StoreNotificationAsync(notification);

        // Assert
        await _mockDatabase.Received(1).KeyExpireAsync(
            "test_notifications:user-1", 
            TimeSpan.FromHours(_options.MaxNotificationAgeHours));
    }

    [Fact]
    public async Task GetNotificationsAsync_WithNoData_ShouldReturnEmptyList()
    {
        // Act
        var notifications = await _storage.GetNotificationsAsync("user-1");

        // Assert
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithCorruptedData_ShouldReturnEmptyListAndClearKey()
    {
        // Arrange
        var key = "test_notifications:user-1";
        _redisStorage[key] = "invalid json";

        // Act
        var notifications = await _storage.GetNotificationsAsync("user-1");

        // Assert
        Assert.Empty(notifications);
        await _mockDatabase.Received(1).KeyDeleteAsync(key);
    }

    [Fact]
    public async Task UpdateNotificationAsync_ShouldUpdateExistingNotification()
    {
        // Arrange
        var notification = new Notification
        {
            Id = "test-1",
            Message = "Original message",
            UserId = "user-1",
            IsRead = false
        };

        await _storage.StoreNotificationAsync(notification);

        // Act
        notification.IsRead = true;
        notification.Message = "Updated message";
        await _storage.UpdateNotificationAsync(notification);

        // Assert
        var retrieved = await _storage.GetNotificationAsync("test-1", "user-1");
        Assert.NotNull(retrieved);
        Assert.True(retrieved.IsRead);
        Assert.Equal("Updated message", retrieved.Message);
    }

    [Fact]
    public async Task RemoveNotificationAsync_ShouldRemoveSpecificNotification()
    {
        // Arrange
        var notification1 = new Notification { Id = "test-1", UserId = "user-1" };
        var notification2 = new Notification { Id = "test-2", UserId = "user-1" };

        await _storage.StoreNotificationAsync(notification1);
        await _storage.StoreNotificationAsync(notification2);

        // Act
        await _storage.RemoveNotificationAsync("test-1", "user-1");

        // Assert
        var notifications = await _storage.GetNotificationsAsync("user-1");
        Assert.Single(notifications);
        Assert.Equal("test-2", notifications.First().Id);
    }

    [Fact]
    public async Task RemoveNotificationsByContextAsync_ShouldRemoveMatchingNotifications()
    {
        // Arrange
        var notifications = new[]
        {
            new Notification { Id = "1", Context = "context-a", UserId = "user-1" },
            new Notification { Id = "2", Context = "context-b", UserId = "user-1" },
            new Notification { Id = "3", Context = "context-a", UserId = "user-1" }
        };

        foreach (var notification in notifications)
        {
            await _storage.StoreNotificationAsync(notification);
        }

        // Act
        await _storage.RemoveNotificationsByContextAsync("context-a", "user-1");

        // Assert
        var remaining = await _storage.GetNotificationsAsync("user-1");
        Assert.Single(remaining);
        Assert.Equal("2", remaining.First().Id);
    }

    [Fact]
    public async Task RemoveNotificationsByCategoryAsync_ShouldRemoveMatchingNotifications()
    {
        // Arrange
        var notifications = new[]
        {
            new Notification { Id = "1", Category = "uploads", UserId = "user-1" },
            new Notification { Id = "2", Category = "downloads", UserId = "user-1" },
            new Notification { Id = "3", Category = "uploads", UserId = "user-1" }
        };

        foreach (var notification in notifications)
        {
            await _storage.StoreNotificationAsync(notification);
        }

        // Act
        await _storage.RemoveNotificationsByCategoryAsync("uploads", "user-1");

        // Assert
        var remaining = await _storage.GetNotificationsAsync("user-1");
        Assert.Single(remaining);
        Assert.Equal("2", remaining.First().Id);
    }

    [Fact]
    public async Task ClearAllNotificationsAsync_ShouldRemoveAllNotifications()
    {
        // Arrange
        var notifications = new[]
        {
            new Notification { Id = "1", UserId = "user-1" },
            new Notification { Id = "2", UserId = "user-1" }
        };

        foreach (var notification in notifications)
        {
            await _storage.StoreNotificationAsync(notification);
        }

        // Act
        await _storage.ClearAllNotificationsAsync("user-1");

        // Assert
        await _mockDatabase.Received(1).KeyDeleteAsync("test_notifications:user-1");
    }

    [Fact]
    public async Task GetNotificationAsync_WithValidId_ShouldReturnNotification()
    {
        // Arrange
        var notification = new Notification
        {
            Id = "test-1",
            Message = "Test message",
            UserId = "user-1"
        };

        await _storage.StoreNotificationAsync(notification);

        // Act
        var retrieved = await _storage.GetNotificationAsync("test-1", "user-1");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("test-1", retrieved.Id);
        Assert.Equal("Test message", retrieved.Message);
    }

    [Fact]
    public async Task GetNotificationAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var retrieved = await _storage.GetNotificationAsync("non-existent", "user-1");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetNotificationAsync_WithNoUserData_ShouldReturnNull()
    {
        // Act
        var retrieved = await _storage.GetNotificationAsync("test-1", "non-existent-user");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public void Constructor_WithNullConnectionMultiplexer_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RedisNotificationStorage(null!, Microsoft.Extensions.Options.Options.Create(_options)));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RedisNotificationStorage(_mockConnectionMultiplexer, null!));
    }

    [Fact]
    public async Task UpdateNotificationAsync_WithNonExistentNotification_ShouldNotThrow()
    {
        // Arrange
        var notification = new Notification
        {
            Id = "non-existent",
            UserId = "user-1"
        };

        // Act & Assert
        await _storage.UpdateNotificationAsync(notification); // Should not throw
        
        // Verify no changes were made
        var notifications = await _storage.GetNotificationsAsync("user-1");
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task StoreNotificationAsync_WithOptionsMaxAgeZero_ShouldNotSetExpiration()
    {
        // Arrange
        var optionsWithoutExpiry = new NotificationServiceOptions 
        { 
            MaxNotificationAgeHours = 0,
            RedisKeyPrefix = "test_notifications:"
        };
        
        var mockOptionsWithoutExpiry = Microsoft.Extensions.Options.Options.Create(optionsWithoutExpiry);
        var storageWithoutExpiry = new RedisNotificationStorage(_mockConnectionMultiplexer, mockOptionsWithoutExpiry);

        var notification = new Notification
        {
            Id = "test-1",
            UserId = "user-1"
        };

        // Act
        await storageWithoutExpiry.StoreNotificationAsync(notification);

        // Assert
        await _mockDatabase.DidNotReceive().KeyExpireAsync(Arg.Any<RedisKey>(), Arg.Any<TimeSpan>());
    }
}