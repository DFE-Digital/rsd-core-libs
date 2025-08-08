using System.Text;
using DfE.CoreLibs.Notifications.Models;
using DfE.CoreLibs.Notifications.Options;
using DfE.CoreLibs.Notifications.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace DfE.CoreLibs.Notifications.Tests.Storage;

public class SessionNotificationStorageTests : StorageTestsBase
{
    private readonly SessionNotificationStorage _storage;
    private readonly IHttpContextAccessor _mockHttpContextAccessor;
    private readonly TestSession _session;
    private readonly HttpContext _mockHttpContext;

    public SessionNotificationStorageTests()
    {
        var options = CreateTestOptions();
        _mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _mockHttpContext = Substitute.For<HttpContext>();
        _session = new TestSession();
        
        _mockHttpContext.Session.Returns(_session);
        _mockHttpContextAccessor.HttpContext.Returns(_mockHttpContext);
        
        _storage = new SessionNotificationStorage(_mockHttpContextAccessor, Microsoft.Extensions.Options.Options.Create(options));
    }

    [Fact]
    public void Constructor_WithNullHttpContextAccessor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SessionNotificationStorage(null!, Microsoft.Extensions.Options.Options.Create(CreateTestOptions())));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SessionNotificationStorage(_mockHttpContextAccessor, null!));
    }

    [Fact]
    public async Task BasicOperations_ShouldWorkCorrectly()
    {
        await AssertBasicStorageOperations(_storage);
    }

    [Fact]
    public async Task StoreNotificationAsync_WithNoHttpContext_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockHttpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var notification = new Notification { Id = "test", UserId = "user1" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _storage.StoreNotificationAsync(notification));
    }

    [Fact]
    public async Task StoreNotificationAsync_WithNoSession_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockHttpContext.Session.Returns((ISession?)null);
        var notification = new Notification { Id = "test", UserId = "user1" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _storage.StoreNotificationAsync(notification));
    }

    [Fact]
    public async Task GetNotificationsFromSession_WithCorruptedData_ShouldReturnEmptyListAndClearSession()
    {
        // Arrange
        _session.SetString("UserNotifications", "{ invalid json }");

        // Act
        var result = await _storage.GetNotificationsAsync("user1");

        // Assert
        Assert.Empty(result);
        Assert.Null(_session.GetString("UserNotifications")); // Should be cleared
    }

    [Fact]
    public async Task StoreNotificationAsync_WithMaxLimitExceeded_ShouldKeepOnlyNewest()
    {
        // Arrange - Add 4 notifications (over limit of 3)
        for (int i = 1; i <= 4; i++)
        {
            var notification = new Notification 
            { 
                Id = $"id-{i}", 
                UserId = "user1", 
                Message = $"Message {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            await _storage.StoreNotificationAsync(notification);
        }

        // Act
        var notifications = await _storage.GetNotificationsAsync("user1");

        // Assert
        Assert.Equal(3, notifications.Count());
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
        Assert.DoesNotContain(remaining, n => n.Context == "context-a");
    }

    // Test session implementation for mocking
    private class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _data = new();

        public string Id => "test-session";
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _data.Keys;

        public void Clear() => _data.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _data.Remove(key);

        public void Set(string key, byte[] value) => _data[key] = value;

        public bool TryGetValue(string key, out byte[]? value) => _data.TryGetValue(key, out value);

        public void SetString(string key, string value) => Set(key, Encoding.UTF8.GetBytes(value));

        public string? GetString(string key) => 
            TryGetValue(key, out var value) && value != null ? Encoding.UTF8.GetString(value) : null;
    }
}