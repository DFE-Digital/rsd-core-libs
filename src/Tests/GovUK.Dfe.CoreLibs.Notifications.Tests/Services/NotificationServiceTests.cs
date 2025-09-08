using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Notifications.Interfaces;
using GovUK.Dfe.CoreLibs.Notifications.Models;
using GovUK.Dfe.CoreLibs.Notifications.Options;
using GovUK.Dfe.CoreLibs.Notifications.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.Notifications.Tests.Services;

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
        _mockContextProvider.GetCurrentUserId().Returns("test-user");
        _mockContextProvider.IsContextAvailable().Returns(true);

        _service = new NotificationService(_mockStorage, _mockContextProvider, _mockOptions, _mockLogger);
    }

    [Fact]
    public void Constructor_WithNullStorage_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new NotificationService(null!, _mockContextProvider, _mockOptions, _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullContextProvider_ShouldThrowArgumentNullException()
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

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task AddNotificationAsync_WithNullOrEmptyMessage_ShouldThrowArgumentException(string message)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.AddNotificationAsync(message, NotificationType.Success));
    }

    [Fact]
    public async Task AddSuccessAsync_ShouldCreateSuccessNotification()
    {
        // Act
        await _service.AddSuccessAsync("Success message");

        // Assert
        await _mockStorage.Received(1).StoreNotificationAsync(
            Arg.Is<Notification>(n => 
                n.Message == "Success message" && 
                n.Type == NotificationType.Success && 
                n.UserId == "test-user"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddErrorAsync_ShouldCreateErrorNotification()
    {
        // Act
        await _service.AddErrorAsync("Error message");

        // Assert
        await _mockStorage.Received(1).StoreNotificationAsync(
            Arg.Is<Notification>(n => 
                n.Message == "Error message" && 
                n.Type == NotificationType.Error),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddInfoAsync_ShouldCreateInfoNotification()
    {
        // Act
        await _service.AddInfoAsync("Info message");

        // Assert
        await _mockStorage.Received(1).StoreNotificationAsync(
            Arg.Is<Notification>(n => 
                n.Message == "Info message" && 
                n.Type == NotificationType.Info),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddWarningAsync_ShouldCreateWarningNotification()
    {
        // Act
        await _service.AddWarningAsync("Warning message");

        // Assert
        await _mockStorage.Received(1).StoreNotificationAsync(
            Arg.Is<Notification>(n => 
                n.Message == "Warning message" && 
                n.Type == NotificationType.Warning),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddNotificationAsync_WithCustomOptions_ShouldUseProvidedOptions()
    {
        // Arrange
        var options = new NotificationOptions
        {
            Context = "test-context",
            Category = "test-category",
            AutoDismiss = false,
            AutoDismissSeconds = 10,
            UserId = "custom-user",
            Priority = NotificationPriority.High
        };

        // Act
        await _service.AddNotificationAsync("Test message", NotificationType.Success, options);

        // Assert
        await _mockStorage.Received(1).StoreNotificationAsync(
            Arg.Is<Notification>(n => 
                n.Context == "test-context" && 
                n.Category == "test-category" &&
                n.AutoDismiss == false &&
                n.AutoDismissSeconds == 10 &&
                n.UserId == "custom-user" &&
                n.Priority == NotificationPriority.High),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUnreadNotificationsAsync_ShouldReturnFilteredNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = "1", IsRead = false },
            new() { Id = "2", IsRead = true },
            new() { Id = "3", IsRead = false }
        };
        _mockStorage.GetNotificationsAsync("test-user", Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        var result = await _service.GetUnreadNotificationsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, n => Assert.False(n.IsRead));
    }

    [Fact]
    public async Task GetAllNotificationsAsync_ShouldReturnOrderedNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = "1", Priority = NotificationPriority.Low, CreatedAt = DateTime.UtcNow.AddMinutes(-2) },
            new() { Id = "2", Priority = NotificationPriority.High, CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
            new() { Id = "3", Priority = NotificationPriority.High, CreatedAt = DateTime.UtcNow }
        };
        _mockStorage.GetNotificationsAsync("test-user", Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        var result = await _service.GetAllNotificationsAsync();

        // Assert
        var resultList = result.ToList();
        Assert.Equal("3", resultList[0].Id); // High priority, newest
        Assert.Equal("2", resultList[1].Id); // High priority, older
        Assert.Equal("1", resultList[2].Id); // Low priority
    }

    [Fact]
    public async Task GetNotificationsByCategoryAsync_ShouldFilterByCategory()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = "1", Category = "test-category" },
            new() { Id = "2", Category = "other-category" },
            new() { Id = "3", Category = "test-category" }
        };
        _mockStorage.GetNotificationsAsync("test-user", Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        var result = await _service.GetNotificationsByCategoryAsync("test-category");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, n => Assert.Equal("test-category", n.Category));
    }

    [Fact]
    public async Task GetNotificationsByCategoryAsync_WithUnreadOnly_ShouldFilterByReadStatus()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = "1", Category = "test-category", IsRead = false },
            new() { Id = "2", Category = "test-category", IsRead = true },
            new() { Id = "3", Category = "test-category", IsRead = false }
        };
        _mockStorage.GetNotificationsAsync("test-user", Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        var result = await _service.GetNotificationsByCategoryAsync("test-category", unreadOnly: true);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, n => Assert.False(n.IsRead));
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldUpdateNotificationAndStore()
    {
        // Arrange
        var notification = new Notification { Id = "test-id", IsRead = false, UserId = "test-user" };
        _mockStorage.GetNotificationAsync("test-id", "test-user", Arg.Any<CancellationToken>())
            .Returns(notification);

        // Act
        await _service.MarkAsReadAsync("test-id");

        // Assert
        await _mockStorage.Received(1).UpdateNotificationAsync(
            Arg.Is<Notification>(n => n.Id == "test-id" && n.IsRead == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldUpdateAllUnreadNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = "1", IsRead = false, UserId = "test-user" },
            new() { Id = "2", IsRead = true, UserId = "test-user" },
            new() { Id = "3", IsRead = false, UserId = "test-user" }
        };
        _mockStorage.GetNotificationsAsync("test-user", Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        await _service.MarkAllAsReadAsync();

        // Assert
        await _mockStorage.Received(2).UpdateNotificationAsync(
            Arg.Is<Notification>(n => n.IsRead == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveNotificationAsync_ShouldRemoveSpecificNotification()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = "test-id", UserId = "test-user" }
        };
        _mockStorage.GetNotificationsAsync("test-user", Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        await _service.RemoveNotificationAsync("test-id");

        // Assert
        await _mockStorage.Received(1).RemoveNotificationAsync("test-id", "test-user", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearAllNotificationsAsync_ShouldCallStorageClearAll()
    {
        // Act
        await _service.ClearAllNotificationsAsync();

        // Assert
        await _mockStorage.Received(1).ClearAllNotificationsAsync("test-user", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearNotificationsByCategoryAsync_ShouldCallStorageRemoveByCategory()
    {
        // Act
        await _service.ClearNotificationsByCategoryAsync("test-category");

        // Assert
        await _mockStorage.Received(1).RemoveNotificationsByCategoryAsync("test-category", "test-user", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearNotificationsByContextAsync_ShouldCallStorageRemoveByContext()
    {
        // Act
        await _service.ClearNotificationsByContextAsync("test-context");

        // Assert
        await _mockStorage.Received(1).RemoveNotificationsByContextAsync("test-context", "test-user", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUnreadCountAsync_ShouldReturnUnreadCount()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { IsRead = false },
            new() { IsRead = true },
            new() { IsRead = false }
        };
        _mockStorage.GetNotificationsAsync("test-user", Arg.Any<CancellationToken>())
            .Returns(notifications);

        // Act
        var result = await _service.GetUnreadCountAsync();

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task GetUserId_WithProvidedUserId_ShouldReturnProvidedUserId()
    {
        // Act
        await _service.GetAllNotificationsAsync("custom-user");

        // Assert
        await _mockStorage.Received(1).GetNotificationsAsync("custom-user", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUserId_WithoutProvidedUserId_WhenContextAvailable_ShouldUseContextUserId()
    {
        // Act
        await _service.GetAllNotificationsAsync();

        // Assert
        await _mockStorage.Received(1).GetNotificationsAsync("test-user", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUserId_WithoutProvidedUserId_WhenContextNotAvailable_ShouldUseDefaultUserId()
    {
        // Arrange
        _mockContextProvider.IsContextAvailable().Returns(false);
        _mockContextProvider.GetCurrentUserId().Returns("default");

        // Act
        await _service.GetAllNotificationsAsync();

        // Assert
        await _mockStorage.Received(1).GetNotificationsAsync("default", Arg.Any<CancellationToken>());
    }

    // Exception handling tests to cover catch blocks
    [Fact]
    public async Task GetAllNotificationsAsync_WhenStorageThrows_ShouldLogErrorAndReturnEmpty()
    {
        // Arrange
        _mockStorage.GetNotificationsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<Notification>>(new InvalidOperationException("Storage error")));

        // Act
        var result = await _service.GetAllNotificationsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNotificationsByCategoryAsync_WhenStorageThrows_ShouldLogErrorAndReturnEmpty()
    {
        // Arrange
        _mockStorage.GetNotificationsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<Notification>>(new InvalidOperationException("Storage error")));

        // Act
        var result = await _service.GetNotificationsByCategoryAsync("test-category");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenStorageThrows_ShouldLogErrorAndNotThrow()
    {
        // Arrange
        _mockStorage.GetNotificationsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<Notification>>(new InvalidOperationException("Storage error")));

        // Act & Assert (should not throw)
        await _service.MarkAsReadAsync("test-id");
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WhenStorageThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        _mockStorage.GetNotificationsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<Notification>>(new InvalidOperationException("Storage error")));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.MarkAllAsReadAsync());
    }

    [Fact]
    public async Task RemoveNotificationAsync_WhenStorageThrows_ShouldLogErrorAndNotThrow()
    {
        // Arrange
        _mockStorage.GetNotificationsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<Notification>>(new InvalidOperationException("Storage error")));

        // Act & Assert (should not throw)
        await _service.RemoveNotificationAsync("test-id");
    }

    [Fact]
    public async Task AddNotificationAsync_WhenStorageThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        _mockStorage.StoreNotificationAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Storage error")));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.AddSuccessAsync("test message"));
    }

    [Fact]
    public async Task ClearAllNotificationsAsync_WhenStorageThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        _mockStorage.ClearAllNotificationsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Storage error")));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.ClearAllNotificationsAsync());
    }

    [Fact]
    public async Task ClearNotificationsByCategoryAsync_WhenStorageThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        _mockStorage.RemoveNotificationsByCategoryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Storage error")));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.ClearNotificationsByCategoryAsync("test-category"));
    }

    [Fact]
    public async Task GetUnreadCountAsync_WhenStorageThrows_ShouldLogErrorAndReturnZero()
    {
        // Arrange
        _mockStorage.GetNotificationsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<Notification>>(new InvalidOperationException("Storage error")));

        // Act
        var result = await _service.GetUnreadCountAsync();

        // Assert
        Assert.Equal(0, result);
    }
}
