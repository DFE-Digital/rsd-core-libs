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
}