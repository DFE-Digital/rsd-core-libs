using DfE.CoreLibs.Notifications.Models;
using DfE.CoreLibs.Notifications.Options;
using DfE.CoreLibs.Notifications.Interfaces;
using Xunit;

namespace DfE.CoreLibs.Notifications.Tests.Storage;

/// <summary>
/// Base class for storage tests to avoid repetition
/// </summary>
public abstract class StorageTestsBase
{
    protected static NotificationServiceOptions CreateTestOptions() => 
        new() { MaxNotificationsPerUser = 3 };

    protected static List<Notification> CreateTestNotifications(string userId = "user1")
    {
        return new List<Notification>
        {
            new() { Id = "1", UserId = userId, Message = "Test 1", Context = "context-a", Category = "category-x", IsRead = false },
            new() { Id = "2", UserId = userId, Message = "Test 2", Context = "context-b", Category = "category-y", IsRead = true },
            new() { Id = "3", UserId = userId, Message = "Test 3", Context = "context-a", Category = "category-x", IsRead = false },
            new() { Id = "4", UserId = "other-user", Message = "Other user notification" }
        };
    }

    protected static async Task AssertBasicStorageOperations(INotificationStorage storage)
    {
        // Test storing and retrieving a notification
        var notification = new Notification 
        { 
            Id = "test", 
            UserId = "testuser", 
            Message = "Test message",
            Type = NotificationType.Success
        };

        await storage.StoreNotificationAsync(notification);
        var retrieved = await storage.GetNotificationAsync("test", "testuser");
        
        Assert.NotNull(retrieved);
        Assert.Equal("Test message", retrieved.Message);
    }
}