using DfE.CoreLibs.Notifications.Models;
using DfE.CoreLibs.Notifications.Options;
using DfE.CoreLibs.Notifications.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Text.Json;

namespace DfE.CoreLibs.Notifications.Tests.Storage;

public class SessionNotificationStorageTests
{
    private readonly IHttpContextAccessor _mockHttpContextAccessor;
    private readonly TestSession _testSession;
    private readonly NotificationServiceOptions _options;
    private readonly SessionNotificationStorage _storage;

    public SessionNotificationStorageTests()
    {
        _mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _testSession = new TestSession();
        
        _options = new NotificationServiceOptions 
        { 
            MaxNotificationsPerUser = 3,
            SessionKey = "TestNotifications"
        };
        
        var mockOptions = Microsoft.Extensions.Options.Options.Create(_options);
        
        var mockHttpContext = Substitute.For<HttpContext>();
        mockHttpContext.Session.Returns(_testSession);
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        _storage = new SessionNotificationStorage(_mockHttpContextAccessor, mockOptions);
    }

    [Fact]
    public async Task StoreNotificationAsync_ShouldStoreNotificationInSession()
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
        var storedJson = _testSession.GetString(_options.SessionKey);
        Assert.NotNull(storedJson);
        
        var storedNotifications = JsonSerializer.Deserialize<List<Notification>>(storedJson);
        Assert.NotNull(storedNotifications);
        Assert.Single(storedNotifications);
        Assert.Equal("test-1", storedNotifications[0].Id);
        Assert.Equal("Test message", storedNotifications[0].Message);
    }

    [Fact]
    public async Task StoreNotificationAsync_WithSameContext_ShouldReplaceExisting()
    {
        // Arrange
        var notification1 = new Notification
        {
            Id = "test-1",
            Message = "First message",
            Context = "same-context"
        };

        var notification2 = new Notification
        {
            Id = "test-2",
            Message = "Second message",
            Context = "same-context"
        };

        // Act
        await _storage.StoreNotificationAsync(notification1);
        await _storage.StoreNotificationAsync(notification2);

        // Assert
        var notifications = await _storage.GetNotificationsAsync("any-user");
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
                CreatedAt = baseTime.AddMinutes(i) // Later notifications have later timestamps
            };
            await _storage.StoreNotificationAsync(notification);
        }

        // Act
        var notifications = await _storage.GetNotificationsAsync("any-user");

        // Assert
        Assert.Equal(3, notifications.Count()); // Should respect MaxNotificationsPerUser
        
        // Should keep the most recent ones (3, 4, 5)
        var notificationIds = notifications.Select(n => n.Id).ToList();
        Assert.Contains("test-3", notificationIds);
        Assert.Contains("test-4", notificationIds);
        Assert.Contains("test-5", notificationIds);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithNoData_ShouldReturnEmptyList()
    {
        // Act
        var notifications = await _storage.GetNotificationsAsync("any-user");

        // Assert
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithCorruptedData_ShouldReturnEmptyListAndClearSession()
    {
        // Arrange
        _testSession.SetString(_options.SessionKey, "invalid json");

        // Act
        var notifications = await _storage.GetNotificationsAsync("any-user");

        // Assert
        Assert.Empty(notifications);
        
        // Should have cleared the corrupted data
        var sessionValue = _testSession.GetString(_options.SessionKey);
        Assert.Null(sessionValue);
    }

    [Fact]
    public async Task UpdateNotificationAsync_ShouldUpdateExistingNotification()
    {
        // Arrange
        var notification = new Notification
        {
            Id = "test-1",
            Message = "Original message",
            IsRead = false
        };

        await _storage.StoreNotificationAsync(notification);

        // Act
        notification.IsRead = true;
        notification.Message = "Updated message";
        await _storage.UpdateNotificationAsync(notification);

        // Assert
        var retrieved = await _storage.GetNotificationAsync("test-1", "any-user");
        Assert.NotNull(retrieved);
        Assert.True(retrieved.IsRead);
        Assert.Equal("Updated message", retrieved.Message);
    }

    [Fact]
    public async Task RemoveNotificationAsync_ShouldRemoveSpecificNotification()
    {
        // Arrange
        var notification1 = new Notification { Id = "test-1" };
        var notification2 = new Notification { Id = "test-2" };

        await _storage.StoreNotificationAsync(notification1);
        await _storage.StoreNotificationAsync(notification2);

        // Act
        await _storage.RemoveNotificationAsync("test-1", "any-user");

        // Assert
        var notifications = await _storage.GetNotificationsAsync("any-user");
        Assert.Single(notifications);
        Assert.Equal("test-2", notifications.First().Id);
    }

    [Fact]
    public async Task RemoveNotificationsByContextAsync_ShouldRemoveMatchingNotifications()
    {
        // Arrange
        var notifications = new[]
        {
            new Notification { Id = "1", Context = "context-a" },
            new Notification { Id = "2", Context = "context-b" },
            new Notification { Id = "3", Context = "context-a" }
        };

        foreach (var notification in notifications)
        {
            await _storage.StoreNotificationAsync(notification);
        }

        // Act
        await _storage.RemoveNotificationsByContextAsync("context-a", "any-user");

        // Assert
        var remaining = await _storage.GetNotificationsAsync("any-user");
        Assert.Single(remaining);
        Assert.Equal("2", remaining.First().Id);
    }

    [Fact]
    public async Task RemoveNotificationsByCategoryAsync_ShouldRemoveMatchingNotifications()
    {
        // Arrange
        var notifications = new[]
        {
            new Notification { Id = "1", Category = "uploads" },
            new Notification { Id = "2", Category = "downloads" },
            new Notification { Id = "3", Category = "uploads" }
        };

        foreach (var notification in notifications)
        {
            await _storage.StoreNotificationAsync(notification);
        }

        // Act
        await _storage.RemoveNotificationsByCategoryAsync("uploads", "any-user");

        // Assert
        var remaining = await _storage.GetNotificationsAsync("any-user");
        Assert.Single(remaining);
        Assert.Equal("2", remaining.First().Id);
    }

    [Fact]
    public async Task ClearAllNotificationsAsync_ShouldRemoveAllNotifications()
    {
        // Arrange
        var notifications = new[]
        {
            new Notification { Id = "1" },
            new Notification { Id = "2" }
        };

        foreach (var notification in notifications)
        {
            await _storage.StoreNotificationAsync(notification);
        }

        // Act
        await _storage.ClearAllNotificationsAsync("any-user");

        // Assert
        var sessionValue = _testSession.GetString(_options.SessionKey);
        Assert.Null(sessionValue);
        
        var remaining = await _storage.GetNotificationsAsync("any-user");
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task GetNotificationAsync_WithValidId_ShouldReturnNotification()
    {
        // Arrange
        var notification = new Notification
        {
            Id = "test-1",
            Message = "Test message"
        };

        await _storage.StoreNotificationAsync(notification);

        // Act
        var retrieved = await _storage.GetNotificationAsync("test-1", "any-user");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("test-1", retrieved.Id);
        Assert.Equal("Test message", retrieved.Message);
    }

    [Fact]
    public async Task GetNotificationAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var retrieved = await _storage.GetNotificationAsync("non-existent", "any-user");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public void Constructor_WithNullHttpContextAccessor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SessionNotificationStorage(null!, Microsoft.Extensions.Options.Options.Create(_options)));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SessionNotificationStorage(_mockHttpContextAccessor, null!));
    }

    [Fact]
    public async Task StoreNotificationAsync_WithNoSession_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockHttpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var notification = new Notification { Id = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _storage.StoreNotificationAsync(notification));
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
    public async Task GetNotificationAsync_WithNonExistentNotification_ShouldReturnNull()
    {
        // Act
        var result = await _storage.GetNotificationAsync("non-existent", "user1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetNotificationsFromSession_WithCorruptedData_ShouldReturnEmptyList()
    {
        // Arrange
        var session = new TestSession();
        session.SetString(_options.SessionKey, "{ invalid json }");
        
        var mockHttpContext = Substitute.For<HttpContext>();
        mockHttpContext.Session.Returns(session);
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        // Act
        var result = await _storage.GetNotificationsAsync("user1");

        // Assert
        Assert.Empty(result);
        // Session should be cleared of corrupted data
        Assert.Null(session.GetString(_options.SessionKey));
    }

    [Fact]
    public async Task StoreNotificationAsync_WithCorruptedExistingData_ShouldClearAndStoreNew()
    {
        // Arrange
        var session = new TestSession();
        session.SetString(_options.SessionKey, "{ invalid json }");
        
        var mockHttpContext = Substitute.For<HttpContext>();
        mockHttpContext.Session.Returns(session);
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);
        
        var notification = new Notification { Id = "new", UserId = "user1" };

        // Act
        await _storage.StoreNotificationAsync(notification);

        // Assert
        var storedData = session.GetString(_options.SessionKey);
        Assert.NotNull(storedData);
        
        var notifications = JsonSerializer.Deserialize<List<Notification>>(storedData);
        Assert.Single(notifications);
        Assert.Equal("new", notifications![0].Id);
    }
}

// Test implementation of ISession that mimics session behavior
public class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _sessionStorage = new();
    
    public string Id => "test-session-id";
    public bool IsAvailable => true;
    public IEnumerable<string> Keys => _sessionStorage.Keys;

    public void Clear() => _sessionStorage.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _sessionStorage.Remove(key);

    public void Set(string key, byte[] value) => _sessionStorage[key] = value;

    public bool TryGetValue(string key, out byte[]? value) => _sessionStorage.TryGetValue(key, out value);
}