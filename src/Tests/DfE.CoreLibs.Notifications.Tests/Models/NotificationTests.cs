using DfE.CoreLibs.Notifications.Models;

namespace DfE.CoreLibs.Notifications.Tests.Models;

public class NotificationTests
{
    [Fact]
    public void Notification_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var notification = new Notification();

        // Assert
        Assert.NotNull(notification.Id);
        Assert.NotEmpty(notification.Id);
        Assert.Equal(string.Empty, notification.Message);
        Assert.Equal(NotificationType.Success, notification.Type);
        Assert.True(notification.CreatedAt <= DateTime.UtcNow);
        Assert.True(notification.CreatedAt > DateTime.UtcNow.AddSeconds(-5)); // Should be recent
        Assert.False(notification.IsRead);
        Assert.True(notification.AutoDismiss);
        Assert.Equal(5, notification.AutoDismissSeconds);
        Assert.Null(notification.Context);
        Assert.Null(notification.Category);
        Assert.Null(notification.UserId);
        Assert.Null(notification.ActionUrl);
        Assert.Null(notification.Metadata);
        Assert.Equal(NotificationPriority.Normal, notification.Priority);
    }

    [Fact]
    public void Notification_SetProperties_WorksCorrectly()
    {
        // Arrange
        var notification = new Notification();
        var metadata = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        notification.Id = "test-id";
        notification.Message = "Test message";
        notification.Type = NotificationType.Error;
        notification.CreatedAt = new DateTime(2023, 1, 1);
        notification.IsRead = true;
        notification.AutoDismiss = false;
        notification.AutoDismissSeconds = 10;
        notification.Context = "test-context";
        notification.Category = "test-category";
        notification.UserId = "test-user";
        notification.ActionUrl = "/test/url";
        notification.Metadata = metadata;
        notification.Priority = NotificationPriority.High;

        // Assert
        Assert.Equal("test-id", notification.Id);
        Assert.Equal("Test message", notification.Message);
        Assert.Equal(NotificationType.Error, notification.Type);
        Assert.Equal(new DateTime(2023, 1, 1), notification.CreatedAt);
        Assert.True(notification.IsRead);
        Assert.False(notification.AutoDismiss);
        Assert.Equal(10, notification.AutoDismissSeconds);
        Assert.Equal("test-context", notification.Context);
        Assert.Equal("test-category", notification.Category);
        Assert.Equal("test-user", notification.UserId);
        Assert.Equal("/test/url", notification.ActionUrl);
        Assert.Same(metadata, notification.Metadata);
        Assert.Equal(NotificationPriority.High, notification.Priority);
    }
}