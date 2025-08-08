using DfE.CoreLibs.Notifications.Interfaces;
using DfE.CoreLibs.Notifications.Models;
using DfE.CoreLibs.Notifications.Options;
using DfE.CoreLibs.Notifications.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DfE.CoreLibs.Notifications.Tests.Services;

public class NotificationServiceTests
{
    private readonly INotificationStorage _mockStorage;
    private readonly IUserContextProvider _mockContextProvider;
    private readonly IOptions<NotificationServiceOptions> _mockOptions;
    private readonly ILogger<NotificationService> _mockLogger;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _mockStorage = Substitute.For<INotificationStorage>();
        _mockContextProvider = Substitute.For<IUserContextProvider>();
        _mockOptions = Substitute.For<IOptions<NotificationServiceOptions>>();
        _mockLogger = Substitute.For<ILogger<NotificationService>>();

        var options = new NotificationServiceOptions();
        _mockOptions.Value.Returns(options);
        _mockContextProvider.GetCurrentUserId().Returns("test-user-123");
        _mockContextProvider.IsContextAvailable().Returns(true);

        _service = new NotificationService(_mockStorage, _mockContextProvider, _mockOptions, _mockLogger);
    }

    [Fact]
    public async Task AddSuccessAsync_ShouldCreateSuccessNotification()
    {
        // Arrange
        const string message = "Operation completed successfully";
        Notification? capturedNotification = null;
        
        await _mockStorage.StoreNotificationAsync(
            Arg.Do<Notification>(n => capturedNotification = n),
            Arg.Any<CancellationToken>());

        // Act
        await _service.AddSuccessAsync(message);

        // Assert
        await _mockStorage.Received(1).StoreNotificationAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        
        Assert.NotNull(capturedNotification);
        Assert.Equal(message, capturedNotification.Message);
        Assert.Equal(NotificationType.Success, capturedNotification.Type);
        Assert.Equal("test-user-123", capturedNotification.UserId);
        Assert.True(capturedNotification.AutoDismiss);
    }

    [Fact]
    public async Task AddErrorAsync_ShouldCreateErrorNotification()
    {
        // Arrange
        const string message = "An error occurred";
        Notification? capturedNotification = null;
        
        await _mockStorage.StoreNotificationAsync(
            Arg.Do<Notification>(n => capturedNotification = n),
            Arg.Any<CancellationToken>());

        // Act
        await _service.AddErrorAsync(message);

        // Assert
        await _mockStorage.Received(1).StoreNotificationAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        
        Assert.NotNull(capturedNotification);
        Assert.Equal(message, capturedNotification.Message);
        Assert.Equal(NotificationType.Error, capturedNotification.Type);
        Assert.Equal("test-user-123", capturedNotification.UserId);
    }

    [Fact]
    public async Task AddNotificationAsync_WithOptions_ShouldApplyOptions()
    {
        // Arrange
        const string message = "Test notification";
        var options = new NotificationOptions
        {
            Context = "test-context",
            Category = "test-category",
            AutoDismiss = false,
            Priority = NotificationPriority.High,
            ActionUrl = "/test/action"
        };
        
        Notification? capturedNotification = null;
        await _mockStorage.StoreNotificationAsync(
            Arg.Do<Notification>(n => capturedNotification = n),
            Arg.Any<CancellationToken>());

        // Act
        await _service.AddNotificationAsync(message, NotificationType.Info, options);

        // Assert
        Assert.NotNull(capturedNotification);
        Assert.Equal(message, capturedNotification.Message);
        Assert.Equal(NotificationType.Info, capturedNotification.Type);
        Assert.Equal("test-context", capturedNotification.Context);
        Assert.Equal("test-category", capturedNotification.Category);
        Assert.False(capturedNotification.AutoDismiss);
        Assert.Equal(NotificationPriority.High, capturedNotification.Priority);
        Assert.Equal("/test/action", capturedNotification.ActionUrl);
    }

    [Fact]
    public async Task GetUnreadNotificationsAsync_ShouldReturnFilteredNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = "1", Message = "Unread 1", IsRead = false, Priority = NotificationPriority.High },
            new() { Id = "2", Message = "Read", IsRead = true },
            new() { Id = "3", Message = "Unread 2", IsRead = false, Priority = NotificationPriority.Normal }
        };

        _mockStorage.GetNotificationsAsync("test-user-123", Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        var result = await _service.GetUnreadNotificationsAsync();

        // Assert
        var unreadNotifications = result.ToList();
        Assert.Equal(2, unreadNotifications.Count);
        Assert.All(unreadNotifications, n => Assert.False(n.IsRead));
        
        // Should be ordered by priority then by created date (descending)
        Assert.Equal("1", unreadNotifications[0].Id); // High priority first
        Assert.Equal("3", unreadNotifications[1].Id);
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldUpdateNotification()
    {
        // Arrange
        const string notificationId = "test-id";
        var notification = new Notification { Id = notificationId, IsRead = false };
        
        _mockStorage.GetNotificationAsync(notificationId, "test-user-123", Arg.Any<CancellationToken>())
            .Returns(notification);

        // Act
        await _service.MarkAsReadAsync(notificationId);

        // Assert
        await _mockStorage.Received(1).UpdateNotificationAsync(notification, Arg.Any<CancellationToken>());
        Assert.True(notification.IsRead);
    }

    [Fact]
    public async Task ClearNotificationsByCategoryAsync_ShouldCallStorageMethod()
    {
        // Arrange
        const string category = "test-category";

        // Act
        await _service.ClearNotificationsByCategoryAsync(category);

        // Assert
        await _mockStorage.Received(1).RemoveNotificationsByCategoryAsync(category, "test-user-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUnreadCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { IsRead = false },
            new() { IsRead = true },
            new() { IsRead = false },
            new() { IsRead = false }
        };

        _mockStorage.GetNotificationsAsync("test-user-123", Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        var count = await _service.GetUnreadCountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task AddNotificationAsync_WithNullMessage_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.AddNotificationAsync(null!, NotificationType.Info));
    }

    [Fact]
    public async Task AddNotificationAsync_WithEmptyMessage_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.AddNotificationAsync("", NotificationType.Info));
    }

    [Fact]
    public async Task Constructor_WithNullStorage_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new NotificationService(null!, _mockContextProvider, _mockOptions, _mockLogger));
    }

    [Fact]
    public async Task Constructor_WithNullContextProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new NotificationService(_mockStorage, null!, _mockOptions, _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new NotificationService(_mockStorage, _mockContextProvider, null!, _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new NotificationService(_mockStorage, _mockContextProvider, _mockOptions, null!));
    }

    [Fact]
    public async Task AddInfoAsync_ShouldCreateInfoNotification()
    {
        // Arrange
        const string message = "Information message";
        Notification? capturedNotification = null;
        
        await _mockStorage.StoreNotificationAsync(
            Arg.Do<Notification>(n => capturedNotification = n),
            Arg.Any<CancellationToken>());

        // Act
        await _service.AddInfoAsync(message);

        // Assert
        await _mockStorage.Received(1).StoreNotificationAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        
        Assert.NotNull(capturedNotification);
        Assert.Equal(message, capturedNotification.Message);
        Assert.Equal(NotificationType.Info, capturedNotification.Type);
        Assert.Equal("test-user-123", capturedNotification.UserId);
    }

    [Fact]
    public async Task AddWarningAsync_ShouldCreateWarningNotification()
    {
        // Arrange
        const string message = "Warning message";
        Notification? capturedNotification = null;
        
        await _mockStorage.StoreNotificationAsync(
            Arg.Do<Notification>(n => capturedNotification = n),
            Arg.Any<CancellationToken>());

        // Act
        await _service.AddWarningAsync(message);

        // Assert
        await _mockStorage.Received(1).StoreNotificationAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        
        Assert.NotNull(capturedNotification);
        Assert.Equal(message, capturedNotification.Message);
        Assert.Equal(NotificationType.Warning, capturedNotification.Type);
        Assert.Equal("test-user-123", capturedNotification.UserId);
    }

    [Fact]
    public async Task GetAllNotificationsAsync_ShouldReturnAllNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = "1", Message = "First", IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-2) },
            new() { Id = "2", Message = "Second", IsRead = true, CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
            new() { Id = "3", Message = "Third", IsRead = false, CreatedAt = DateTime.UtcNow }
        };

        _mockStorage.GetNotificationsAsync("test-user-123", Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        var result = await _service.GetAllNotificationsAsync();

        // Assert
        var allNotifications = result.ToList();
        Assert.Equal(3, allNotifications.Count);
        // Should be ordered by priority then by created date (descending)
        Assert.Equal("3", allNotifications[0].Id); // Most recent first
        Assert.Equal("2", allNotifications[1].Id);
        Assert.Equal("1", allNotifications[2].Id);
    }

    [Fact]
    public async Task GetNotificationsByCategoryAsync_ShouldReturnFilteredNotifications()
    {
        // Arrange
        const string category = "test-category";
        var notifications = new List<Notification>
        {
            new() { Id = "1", Category = category, IsRead = false },
            new() { Id = "2", Category = "other-category", IsRead = false },
            new() { Id = "3", Category = category, IsRead = true }
        };

        _mockStorage.GetNotificationsAsync("test-user-123", Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        var result = await _service.GetNotificationsByCategoryAsync(category, unreadOnly: true);

        // Assert
        var filteredNotifications = result.ToList();
        Assert.Single(filteredNotifications);
        Assert.Equal("1", filteredNotifications[0].Id);
    }

    [Fact]
    public async Task GetNotificationsByCategoryAsync_WithUnreadOnlyFalse_ShouldReturnAllInCategory()
    {
        // Arrange
        const string category = "test-category";
        var notifications = new List<Notification>
        {
            new() { Id = "1", Category = category, IsRead = false },
            new() { Id = "2", Category = "other-category", IsRead = false },
            new() { Id = "3", Category = category, IsRead = true }
        };

        _mockStorage.GetNotificationsAsync("test-user-123", Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        var result = await _service.GetNotificationsByCategoryAsync(category, unreadOnly: false);

        // Assert
        var filteredNotifications = result.ToList();
        Assert.Equal(2, filteredNotifications.Count);
        Assert.Contains(filteredNotifications, n => n.Id == "1");
        Assert.Contains(filteredNotifications, n => n.Id == "3");
    }

    [Fact]
    public async Task MarkAsReadAsync_WithNotFoundNotification_ShouldNotThrow()
    {
        // Arrange
        const string notificationId = "non-existent-id";
        
        _mockStorage.GetNotificationAsync(notificationId, "test-user-123", Arg.Any<CancellationToken>())
            .Returns((Notification?)null);

        // Act & Assert - Should not throw
        await _service.MarkAsReadAsync(notificationId);

        // Assert - UpdateNotificationAsync should not be called
        await _mockStorage.DidNotReceive().UpdateNotificationAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldUpdateAllUnreadNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = "1", IsRead = false },
            new() { Id = "2", IsRead = true },
            new() { Id = "3", IsRead = false }
        };

        _mockStorage.GetNotificationsAsync("test-user-123", Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        await _service.MarkAllAsReadAsync();

        // Assert
        await _mockStorage.Received(2).UpdateNotificationAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        Assert.True(notifications[0].IsRead);
        Assert.True(notifications[1].IsRead); // Already was true
        Assert.True(notifications[2].IsRead);
    }

    [Fact]
    public async Task RemoveNotificationAsync_ShouldCallStorageMethod()
    {
        // Arrange
        const string notificationId = "test-id";

        // Act
        await _service.RemoveNotificationAsync(notificationId);

        // Assert
        await _mockStorage.Received(1).RemoveNotificationAsync(notificationId, "test-user-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearAllNotificationsAsync_ShouldCallStorageMethod()
    {
        // Act
        await _service.ClearAllNotificationsAsync();

        // Assert
        await _mockStorage.Received(1).ClearAllNotificationsAsync("test-user-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearNotificationsByContextAsync_ShouldCallStorageMethod()
    {
        // Arrange
        const string context = "test-context";

        // Act
        await _service.ClearNotificationsByContextAsync(context);

        // Assert
        await _mockStorage.Received(1).RemoveNotificationsByContextAsync(context, "test-user-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddNotificationAsync_WithUserIdInOptions_ShouldUseProvidedUserId()
    {
        // Arrange
        const string message = "Test notification";
        const string providedUserId = "custom-user-id";
        var options = new NotificationOptions { UserId = providedUserId };
        
        Notification? capturedNotification = null;
        await _mockStorage.StoreNotificationAsync(
            Arg.Do<Notification>(n => capturedNotification = n),
            Arg.Any<CancellationToken>());

        // Act
        await _service.AddNotificationAsync(message, NotificationType.Info, options);

        // Assert
        Assert.NotNull(capturedNotification);
        Assert.Equal(providedUserId, capturedNotification.UserId);
    }

    [Fact]
    public async Task AddNotificationAsync_WithMetadata_ShouldIncludeMetadata()
    {
        // Arrange
        const string message = "Test notification";
        var metadata = new Dictionary<string, object> { ["key1"] = "value1", ["key2"] = 42 };
        var options = new NotificationOptions { Metadata = metadata };
        
        Notification? capturedNotification = null;
        await _mockStorage.StoreNotificationAsync(
            Arg.Do<Notification>(n => capturedNotification = n),
            Arg.Any<CancellationToken>());

        // Act
        await _service.AddNotificationAsync(message, NotificationType.Info, options);

        // Assert
        Assert.NotNull(capturedNotification);
        Assert.Same(metadata, capturedNotification.Metadata);
    }

    [Fact]
    public async Task Methods_WithProvidedUserId_ShouldUseProvidedUserId()
    {
        // Arrange
        const string customUserId = "custom-user";

        // Act & Assert for each method that accepts userId
        await _service.GetUnreadNotificationsAsync(customUserId);
        await _mockStorage.Received(1).GetNotificationsAsync(customUserId, Arg.Any<CancellationToken>());

        await _service.GetAllNotificationsAsync(customUserId);
        await _mockStorage.Received(2).GetNotificationsAsync(customUserId, Arg.Any<CancellationToken>());

        await _service.GetNotificationsByCategoryAsync("category", userId: customUserId);
        await _mockStorage.Received(3).GetNotificationsAsync(customUserId, Arg.Any<CancellationToken>());

        await _service.MarkAllAsReadAsync(customUserId);
        await _mockStorage.Received(4).GetNotificationsAsync(customUserId, Arg.Any<CancellationToken>());

        await _service.ClearAllNotificationsAsync(customUserId);
        await _mockStorage.Received(1).ClearAllNotificationsAsync(customUserId, Arg.Any<CancellationToken>());

        await _service.ClearNotificationsByCategoryAsync("category", customUserId);
        await _mockStorage.Received(1).RemoveNotificationsByCategoryAsync("category", customUserId, Arg.Any<CancellationToken>());

        await _service.ClearNotificationsByContextAsync("context", customUserId);
        await _mockStorage.Received(1).RemoveNotificationsByContextAsync("context", customUserId, Arg.Any<CancellationToken>());

        await _service.GetUnreadCountAsync(customUserId);
        await _mockStorage.Received(5).GetNotificationsAsync(customUserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUnreadCountAsync_WhenStorageThrows_ShouldReturnZero()
    {
        // Arrange
        _mockStorage.GetNotificationsAsync("test-user-123", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<Notification>>(new InvalidOperationException("Storage error")));

        // Act
        var result = await _service.GetUnreadCountAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ClearNotificationsByContextAsync_WhenStorageThrows_ShouldRethrow()
    {
        // Arrange
        _mockStorage.RemoveNotificationsByContextAsync("context", "test-user-123", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Storage error")));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.ClearNotificationsByContextAsync("context"));
    }

    [Fact]
    public void GetUserId_WhenContextProviderUnavailable_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockContextProvider.IsContextAvailable().Returns(false);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetUnreadNotificationsAsync());
    }

    [Fact]
    public async Task AddNotificationAsync_WithNullOptions_ShouldUseTypeDefaults()
    {
        // Arrange
        const string message = "Test message";
        Notification? capturedNotification = null;
        
        await _mockStorage.StoreNotificationAsync(
            Arg.Do<Notification>(n => capturedNotification = n),
            Arg.Any<CancellationToken>());

        // Act
        await _service.AddErrorAsync(message, null); // null options should use type defaults

        // Assert
        Assert.NotNull(capturedNotification);
        Assert.False(capturedNotification.AutoDismiss); // Error type default
        Assert.Equal(10, capturedNotification.AutoDismissSeconds); // Error type default
    }

    [Fact]
    public async Task AddNotificationAsync_WithPartialOptions_ShouldPreserveUserSettings()
    {
        // Arrange
        const string message = "Test message";
        var options = new NotificationOptions
        {
            Context = "custom-context",
            AutoDismiss = true, // Different from Error default (false)
            AutoDismissSeconds = 15 // Different from Error default (10)
        };
        
        Notification? capturedNotification = null;
        await _mockStorage.StoreNotificationAsync(
            Arg.Do<Notification>(n => capturedNotification = n),
            Arg.Any<CancellationToken>());

        // Act
        await _service.AddErrorAsync(message, options);

        // Assert
        Assert.NotNull(capturedNotification);
        Assert.Equal("custom-context", capturedNotification.Context);
        Assert.True(capturedNotification.AutoDismiss); // User's setting preserved
        Assert.Equal(15, capturedNotification.AutoDismissSeconds); // User's setting preserved
    }
}