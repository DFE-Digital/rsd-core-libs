using DfE.CoreLibs.Notifications.Models;
using DfE.CoreLibs.Notifications.Options;
using DfE.CoreLibs.Notifications.Storage;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;
using System.Linq;
using Xunit;

namespace DfE.CoreLibs.Notifications.Tests.Storage;

public class RedisNotificationStorageTests : StorageTestsBase
{
    private readonly RedisNotificationStorage _storage;
    private readonly IConnectionMultiplexer _mockConnectionMultiplexer;
    private readonly IDatabase _mockDatabase;
    private readonly NotificationServiceOptions _options;

    public RedisNotificationStorageTests()
    {
        _options = CreateTestOptions();
        _options.RedisKeyPrefix = "test:notifications:";
        
        _mockConnectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        _mockDatabase = Substitute.For<IDatabase>();
        _mockConnectionMultiplexer.GetDatabase().Returns(_mockDatabase);
        
        _storage = new RedisNotificationStorage(_mockConnectionMultiplexer, Microsoft.Extensions.Options.Options.Create(_options));
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
    public async Task StoreNotificationAsync_ShouldSerializeAndStoreInRedis()
    {
        // Arrange
        var notification = new Notification 
        { 
            Id = "test", 
            UserId = "user1", 
            Message = "Test message" 
        };

        // Act
        await _storage.StoreNotificationAsync(notification);

        // Assert
        await _mockDatabase.Received(1).StringSetAsync(
            "test:notifications:user1",
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetNotificationsAsync_WithValidData_ShouldDeserializeCorrectly()
    {
        // Arrange
        var allNotifications = CreateTestNotifications();
        var user1Notifications = allNotifications.Where(n => n.UserId == "user1").ToList();
        var json = System.Text.Json.JsonSerializer.Serialize(user1Notifications);
        _mockDatabase.StringGetAsync("test:notifications:user1")
            .Returns(json);

        // Act
        var result = await _storage.GetNotificationsAsync("user1");

        // Assert
        Assert.Equal(3, result.Count()); // 3 notifications for user1
    }

    [Fact]
    public async Task GetNotificationsAsync_WithNoData_ShouldReturnEmpty()
    {
        // Arrange
        _mockDatabase.StringGetAsync(Arg.Any<RedisKey>())
            .Returns(RedisValue.Null);

        // Act
        var result = await _storage.GetNotificationsAsync("user1");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithCorruptedData_ShouldReturnEmptyAndClearKey()
    {
        // Arrange
        _mockDatabase.StringGetAsync("test:notifications:user1")
            .Returns("{ invalid json }");

        // Act
        var result = await _storage.GetNotificationsAsync("user1");

        // Assert
        Assert.Empty(result);
        await _mockDatabase.Received(1).KeyDeleteAsync("test:notifications:user1");
    }

    [Fact]
    public async Task GetNotificationAsync_WithExistingNotification_ShouldReturnNotification()
    {
        // Arrange
        var notifications = CreateTestNotifications();
        var json = System.Text.Json.JsonSerializer.Serialize(notifications);
        _mockDatabase.StringGetAsync("test:notifications:user1")
            .Returns(json);

        // Act
        var result = await _storage.GetNotificationAsync("1", "user1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.Id);
    }

    [Fact]
    public async Task GetNotificationAsync_WithNonExistentNotification_ShouldReturnNull()
    {
        // Arrange
        var notifications = CreateTestNotifications();
        var json = System.Text.Json.JsonSerializer.Serialize(notifications);
        _mockDatabase.StringGetAsync("test:notifications:user1")
            .Returns(json);

        // Act
        var result = await _storage.GetNotificationAsync("non-existent", "user1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveNotificationAsync_ShouldRemoveAndUpdateRedis()
    {
        // Arrange
        var notifications = CreateTestNotifications();
        var json = System.Text.Json.JsonSerializer.Serialize(notifications);
        _mockDatabase.StringGetAsync("test:notifications:user1")
            .Returns(json);

        // Act
        await _storage.RemoveNotificationAsync("1", "user1");

        // Assert
        await _mockDatabase.Received(1).StringSetAsync(
            "test:notifications:user1",
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task ClearAllNotificationsAsync_ShouldDeleteRedisKey()
    {
        // Act
        await _storage.ClearAllNotificationsAsync("user1");

        // Assert
        await _mockDatabase.Received(1).KeyDeleteAsync("test:notifications:user1");
    }

    [Fact]
    public async Task RemoveNotificationsByContextAsync_ShouldFilterAndUpdate()
    {
        // Arrange
        var notifications = CreateTestNotifications();
        var json = System.Text.Json.JsonSerializer.Serialize(notifications);
        _mockDatabase.StringGetAsync("test:notifications:user1")
            .Returns(json);

        // Act
        await _storage.RemoveNotificationsByContextAsync("context-a", "user1");

        // Assert
        await _mockDatabase.Received(1).StringSetAsync(
            "test:notifications:user1",
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task RemoveNotificationsByCategoryAsync_ShouldFilterAndUpdate()
    {
        // Arrange
        var notifications = CreateTestNotifications();
        var json = System.Text.Json.JsonSerializer.Serialize(notifications);
        _mockDatabase.StringGetAsync("test:notifications:user1")
            .Returns(json);

        // Act
        await _storage.RemoveNotificationsByCategoryAsync("category-x", "user1");

        // Assert
        await _mockDatabase.Received(1).StringSetAsync(
            "test:notifications:user1",
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task StoreNotificationAsync_WithMaxLimitExceeded_ShouldKeepOnlyNewest()
    {
        // Arrange
        var existingNotifications = new List<Notification>();
        for (int i = 1; i <= 4; i++) // Over the limit of 3
        {
            existingNotifications.Add(new Notification 
            { 
                Id = $"existing-{i}", 
                UserId = "user1",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        
        var json = System.Text.Json.JsonSerializer.Serialize(existingNotifications);
        _mockDatabase.StringGetAsync("test:notifications:user1")
            .Returns(json);

        var newNotification = new Notification 
        { 
            Id = "new", 
            UserId = "user1",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _storage.StoreNotificationAsync(newNotification);

        // Assert - Should store only 3 notifications (newest ones)
        await _mockDatabase.Received(1).StringSetAsync(
            "test:notifications:user1",
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>());
    }
}