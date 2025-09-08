using GovUK.Dfe.CoreLibs.Notifications.Models;
using GovUK.Dfe.CoreLibs.Notifications.Options;
using GovUK.Dfe.CoreLibs.Notifications.Storage;
using Microsoft.Extensions.Options;
using Xunit;

namespace GovUK.Dfe.CoreLibs.Notifications.Tests.Storage;

public class InMemoryNotificationStorageTests : StorageTestsBase
{
    private readonly InMemoryNotificationStorage _storage;

    public InMemoryNotificationStorageTests()
    {
        var options = CreateTestOptions();
        _storage = new InMemoryNotificationStorage(Microsoft.Extensions.Options.Options.Create(options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new InMemoryNotificationStorage(null!));
    }

    [Fact]
    public async Task BasicOperations_ShouldWorkCorrectly()
    {
        await AssertBasicStorageOperations(_storage);
    }

    [Fact]
    public async Task StoreNotificationAsync_WithMultipleNotifications_ShouldRespectMaxLimit()
    {
        // Arrange - Add 4 notifications (over limit of 3)
        for (int i = 1; i <= 4; i++)
        {
            var notification = new Notification 
            { 
                Id = $"id-{i}", 
                UserId = "user1", 
                Message = $"Message {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i) // Older notifications first
            };
            await _storage.StoreNotificationAsync(notification);
        }

        // Act
        var notifications = await _storage.GetNotificationsAsync("user1");

        // Assert - Should only have 3 notifications (newest ones)
        Assert.Equal(3, notifications.Count());
        Assert.Contains(notifications, n => n.Id == "id-1"); // Newest
        Assert.Contains(notifications, n => n.Id == "id-2");
        Assert.Contains(notifications, n => n.Id == "id-3");
        Assert.DoesNotContain(notifications, n => n.Id == "id-4"); // Oldest should be removed
    }

    [Fact]
    public async Task GetNotificationAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Act
        var result = await _storage.GetNotificationAsync("non-existent", "user1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithNonExistentUser_ShouldReturnEmpty()
    {
        // Act
        var result = await _storage.GetNotificationsAsync("non-existent-user");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task RemoveNotificationAsync_ShouldRemoveSpecificNotification()
    {
        // Arrange
        var notification = new Notification { Id = "test", UserId = "user1", Message = "Test" };
        await _storage.StoreNotificationAsync(notification);

        // Act
        await _storage.RemoveNotificationAsync("test", "user1");

        // Assert
        var result = await _storage.GetNotificationAsync("test", "user1");
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveNotificationsByContextAsync_ShouldRemoveMatchingNotifications()
    {
        // Arrange
        var notifications = CreateTestNotifications();
        foreach (var notification in notifications)
        {
            await _storage.StoreNotificationAsync(notification);
        }

        // Act
        await _storage.RemoveNotificationsByContextAsync("context-a", "user1");

        // Assert
        var remaining = await _storage.GetNotificationsAsync("user1");
        Assert.Single(remaining); // Only notification with context-b should remain
        Assert.Equal("context-b", remaining.First().Context);
    }

    [Fact]
    public async Task RemoveNotificationsByCategoryAsync_ShouldRemoveMatchingNotifications()
    {
        // Arrange
        var notifications = CreateTestNotifications();
        foreach (var notification in notifications)
        {
            await _storage.StoreNotificationAsync(notification);
        }

        // Act
        await _storage.RemoveNotificationsByCategoryAsync("category-x", "user1");

        // Assert
        var remaining = await _storage.GetNotificationsAsync("user1");
        Assert.Single(remaining); // Only notification with category-y should remain
        Assert.Equal("category-y", remaining.First().Category);
    }

    [Fact]
    public async Task ClearAllNotificationsAsync_ShouldRemoveAllUserNotifications()
    {
        // Arrange
        var notifications = CreateTestNotifications();
        foreach (var notification in notifications)
        {
            await _storage.StoreNotificationAsync(notification);
        }

        // Act
        await _storage.ClearAllNotificationsAsync("user1");

        // Assert
        var user1Notifications = await _storage.GetNotificationsAsync("user1");
        var otherUserNotifications = await _storage.GetNotificationsAsync("other-user");
        
        Assert.Empty(user1Notifications);
        Assert.Single(otherUserNotifications); // Other user's notifications should remain
    }
}
