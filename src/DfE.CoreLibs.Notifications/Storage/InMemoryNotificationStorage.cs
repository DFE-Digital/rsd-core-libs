using System.Collections.Concurrent;
using DfE.CoreLibs.Notifications.Interfaces;
using DfE.CoreLibs.Notifications.Models;
using DfE.CoreLibs.Notifications.Options;
using Microsoft.Extensions.Options;

namespace DfE.CoreLibs.Notifications.Storage;

/// <summary>
/// Notification storage implementation using in-memory storage
/// Note: This implementation is not recommended for production use as data is lost on application restart
/// </summary>
public class InMemoryNotificationStorage : INotificationStorage
{
    private readonly ConcurrentDictionary<string, List<Notification>> _storage = new();
    private readonly NotificationServiceOptions _options;
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the InMemoryNotificationStorage
    /// </summary>
    /// <param name="options">Notification service options</param>
    public InMemoryNotificationStorage(IOptions<NotificationServiceOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Store a notification in memory
    /// </summary>
    /// <param name="notification">Notification to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task StoreNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var userId = notification.UserId ?? "default";
        
        lock (_lockObject)
        {
            var notifications = _storage.GetOrAdd(userId, _ => new List<Notification>());
            
            // Remove existing notifications with same context if specified
            if (!string.IsNullOrEmpty(notification.Context))
            {
                notifications.RemoveAll(n => n.Context == notification.Context);
            }

            notifications.Add(notification);
            
            // Keep only the latest notifications to prevent memory bloat
            if (notifications.Count > _options.MaxNotificationsPerUser)
            {
                var trimmed = notifications.OrderByDescending(n => n.CreatedAt)
                    .Take(_options.MaxNotificationsPerUser)
                    .ToList();
                
                _storage.TryUpdate(userId, trimmed, notifications);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Get all notifications for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All notifications for the user</returns>
    public Task<IEnumerable<Notification>> GetNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(userId, out var notifications);
        return Task.FromResult<IEnumerable<Notification>>(notifications ?? new List<Notification>());
    }

    /// <summary>
    /// Update an existing notification
    /// </summary>
    /// <param name="notification">Updated notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task UpdateNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var userId = notification.UserId ?? "default";
        
        lock (_lockObject)
        {
            if (_storage.TryGetValue(userId, out var notifications))
            {
                var index = notifications.FindIndex(n => n.Id == notification.Id);
                if (index >= 0)
                {
                    notifications[index] = notification;
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Remove a specific notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task RemoveNotificationAsync(string notificationId, string userId, CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            if (_storage.TryGetValue(userId, out var notifications))
            {
                notifications.RemoveAll(n => n.Id == notificationId);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Remove notifications by context
    /// </summary>
    /// <param name="context">Context to match</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task RemoveNotificationsByContextAsync(string context, string userId, CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            if (_storage.TryGetValue(userId, out var notifications))
            {
                notifications.RemoveAll(n => n.Context == context);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Remove notifications by category
    /// </summary>
    /// <param name="category">Category to match</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task RemoveNotificationsByCategoryAsync(string category, string userId, CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            if (_storage.TryGetValue(userId, out var notifications))
            {
                notifications.RemoveAll(n => n.Category == category);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clear all notifications for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task ClearAllNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        _storage.TryRemove(userId, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get a specific notification by ID
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification if found, null otherwise</returns>
    public Task<Notification?> GetNotificationAsync(string notificationId, string userId, CancellationToken cancellationToken = default)
    {
        if (_storage.TryGetValue(userId, out var notifications))
        {
            var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
            return Task.FromResult(notification);
        }

        return Task.FromResult<Notification?>(null);
    }
}