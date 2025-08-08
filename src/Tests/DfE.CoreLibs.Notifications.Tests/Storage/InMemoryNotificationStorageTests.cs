using DfE.CoreLibs.Notifications.Models;
using DfE.CoreLibs.Notifications.Options;
using DfE.CoreLibs.Notifications.Storage;
using Microsoft.Extensions.Options;

namespace DfE.CoreLibs.Notifications.Tests.Storage;

public class InMemoryNotificationStorageTests
{
    private readonly InMemoryNotificationStorage _storage;
    private readonly NotificationServiceOptions _options;

    public InMemoryNotificationStorageTests()
    {
        _options = new NotificationServiceOptions { MaxNotificationsPerUser = 3 };
        var mockOptions = Microsoft.Extensions.Options.Options.Create(_options);
        _storage = new InMemoryNotificationStorage(mockOptions);
    }

    [Fact]
    public async Task StoreNotificationAsync_ShouldStoreNotification()
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
        var notifications = await _storage.GetNotificationsAsync("user-1");
        Assert.Single(notifications);
        Assert.Equal("test-1", notifications.First().Id);
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
                CreatedAt = baseTime.AddMinutes(i) // Later notifications have later timestamps
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
    public async Task ClearAllNotificationsAsync_ShouldRemoveAllUserNotifications()
    {
        // Arrange
        var user1Notifications = new[]
        {
            new Notification { Id = "1", UserId = "user-1" },
            new Notification { Id = "2", UserId = "user-1" }
        };

        var user2Notifications = new[]
        {
            new Notification { Id = "3", UserId = "user-2" }
        };

        foreach (var notification in user1Notifications.Concat(user2Notifications))
        {
            await _storage.StoreNotificationAsync(notification);
        }

        // Act
        await _storage.ClearAllNotificationsAsync("user-1");

        // Assert
        var user1Remaining = await _storage.GetNotificationsAsync("user-1");
        var user2Remaining = await _storage.GetNotificationsAsync("user-2");

        Assert.Empty(user1Remaining);
        Assert.Single(user2Remaining); // User 2's notifications should remain
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
    public async Task GetNotificationsAsync_WithNoNotifications_ShouldReturnEmptyList()
    {
        // Act
        var notifications = await _storage.GetNotificationsAsync("non-existent-user");

        // Assert
        Assert.Empty(notifications);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InMemoryNotificationStorage(null!));
    }

    [Fact]
    public async Task StoreNotificationAsync_WithNullUserId_ShouldUseDefaultUserId()
    {
        // Arrange
        var notification = new Notification { Id = "1", UserId = null };

        // Act
        await _storage.StoreNotificationAsync(notification);

        // Assert
        var notifications = await _storage.GetNotificationsAsync("default");
        Assert.Single(notifications);
    }

    [Fact]
    public async Task RemoveNotificationsByContextAsync_WithNonExistentUser_ShouldNotThrow()
    {
        // Act & Assert - should not throw
        await _storage.RemoveNotificationsByContextAsync("context", "non-existent-user");
    }

    [Fact]
    public async Task RemoveNotificationsByCategoryAsync_WithNonExistentUser_ShouldNotThrow()
    {
        // Act & Assert - should not throw
        await _storage.RemoveNotificationsByCategoryAsync("category", "non-existent-user");
    }

    [Fact]
    public async Task ClearAllNotificationsAsync_WithNonExistentUser_ShouldNotThrow()
    {
        // Act & Assert - should not throw
        await _storage.ClearAllNotificationsAsync("non-existent-user");
    }

    [Fact]
    public async Task RemoveNotificationAsync_WithNonExistentUser_ShouldNotThrow()
    {
        // Act & Assert - should not throw
        await _storage.RemoveNotificationAsync("notification-id", "non-existent-user");
    }

    [Fact]
    public async Task UpdateNotificationAsync_WithNonExistentNotification_ShouldNotThrow()
    {
        // Arrange
        var notification = new Notification { Id = "non-existent", UserId = "user1" };

        // Act & Assert - should not throw
        await _storage.UpdateNotificationAsync(notification);
    }

    [Fact]
    public async Task StoreNotificationAsync_WithEmptyContext_ShouldNotRemoveExistingNotifications()
    {
        // Arrange
        var notification1 = new Notification { Id = "1", UserId = "user1", Context = "context1" };
        var notification2 = new Notification { Id = "2", UserId = "user1", Context = "" };

        // Act
        await _storage.StoreNotificationAsync(notification1);
        await _storage.StoreNotificationAsync(notification2);

        // Assert
        var notifications = await _storage.GetNotificationsAsync("user1");
        Assert.Equal(2, notifications.Count());
    }

    [Fact]
    public async Task StoreNotificationAsync_WithWhitespaceContext_ShouldNotRemoveExistingNotifications()
    {
        // Arrange
        var notification1 = new Notification { Id = "1", UserId = "user1", Context = "context1" };
        var notification2 = new Notification { Id = "2", UserId = "user1", Context = "   " };

        // Act
        await _storage.StoreNotificationAsync(notification1);
        await _storage.StoreNotificationAsync(notification2);

        // Assert
        var notifications = await _storage.GetNotificationsAsync("user1");
        Assert.Equal(2, notifications.Count());
    }
}