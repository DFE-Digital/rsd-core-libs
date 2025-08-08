using System.Text.Json;
using DfE.CoreLibs.Notifications.Interfaces;
using DfE.CoreLibs.Notifications.Models;
using DfE.CoreLibs.Notifications.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DfE.CoreLibs.Notifications.Storage;

/// <summary>
/// Notification storage implementation using Redis
/// </summary>
public class RedisNotificationStorage : INotificationStorage
{
    private readonly IDatabase _database;
    private readonly NotificationServiceOptions _options;

    /// <summary>
    /// Initializes a new instance of the RedisNotificationStorage
    /// </summary>
    /// <param name="connectionMultiplexer">Redis connection multiplexer</param>
    /// <param name="options">Notification service options</param>
    public RedisNotificationStorage(IConnectionMultiplexer connectionMultiplexer, IOptions<NotificationServiceOptions> options)
    {
        _database = connectionMultiplexer?.GetDatabase() ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    private string GetUserKey(string userId) => $"{_options.RedisKeyPrefix}{userId}";

    /// <summary>
    /// Store a notification in Redis
    /// </summary>
    /// <param name="notification">Notification to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task StoreNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var userId = notification.UserId ?? "default";
        var key = GetUserKey(userId);
        
        // Get existing notifications
        var existingNotifications = await GetNotificationsFromRedis(userId);
        
        // Remove existing notifications with same context if specified
        if (!string.IsNullOrEmpty(notification.Context))
        {
            existingNotifications.RemoveAll(n => n.Context == notification.Context);
        }

        existingNotifications.Add(notification);
        
        // Keep only the latest notifications to prevent unlimited growth
        if (existingNotifications.Count > _options.MaxNotificationsPerUser)
        {
            existingNotifications = existingNotifications.OrderByDescending(n => n.CreatedAt)
                .Take(_options.MaxNotificationsPerUser)
                .ToList();
        }

        // Store back to Redis
        var json = JsonSerializer.Serialize(existingNotifications);
        await _database.StringSetAsync(key, json);
        
        // Set expiration if configured
        if (_options.MaxNotificationAgeHours > 0)
        {
            await _database.KeyExpireAsync(key, TimeSpan.FromHours(_options.MaxNotificationAgeHours));
        }
    }

    /// <summary>
    /// Get all notifications for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All notifications for the user</returns>
    public async Task<IEnumerable<Notification>> GetNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var notifications = await GetNotificationsFromRedis(userId);
        return notifications;
    }

    /// <summary>
    /// Update an existing notification
    /// </summary>
    /// <param name="notification">Updated notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task UpdateNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var userId = notification.UserId ?? "default";
        var notifications = await GetNotificationsFromRedis(userId);
        
        var index = notifications.FindIndex(n => n.Id == notification.Id);
        if (index >= 0)
        {
            notifications[index] = notification;
            
            var key = GetUserKey(userId);
            var json = JsonSerializer.Serialize(notifications);
            await _database.StringSetAsync(key, json);
        }
    }

    /// <summary>
    /// Remove a specific notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemoveNotificationAsync(string notificationId, string userId, CancellationToken cancellationToken = default)
    {
        var notifications = await GetNotificationsFromRedis(userId);
        notifications.RemoveAll(n => n.Id == notificationId);
        
        var key = GetUserKey(userId);
        var json = JsonSerializer.Serialize(notifications);
        await _database.StringSetAsync(key, json);
    }

    /// <summary>
    /// Remove notifications by context
    /// </summary>
    /// <param name="context">Context to match</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemoveNotificationsByContextAsync(string context, string userId, CancellationToken cancellationToken = default)
    {
        var notifications = await GetNotificationsFromRedis(userId);
        notifications.RemoveAll(n => n.Context == context);
        
        var key = GetUserKey(userId);
        var json = JsonSerializer.Serialize(notifications);
        await _database.StringSetAsync(key, json);
    }

    /// <summary>
    /// Remove notifications by category
    /// </summary>
    /// <param name="category">Category to match</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemoveNotificationsByCategoryAsync(string category, string userId, CancellationToken cancellationToken = default)
    {
        var notifications = await GetNotificationsFromRedis(userId);
        notifications.RemoveAll(n => n.Category == category);
        
        var key = GetUserKey(userId);
        var json = JsonSerializer.Serialize(notifications);
        await _database.StringSetAsync(key, json);
    }

    /// <summary>
    /// Clear all notifications for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ClearAllNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var key = GetUserKey(userId);
        await _database.KeyDeleteAsync(key);
    }

    /// <summary>
    /// Get a specific notification by ID
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification if found, null otherwise</returns>
    public async Task<Notification?> GetNotificationAsync(string notificationId, string userId, CancellationToken cancellationToken = default)
    {
        var notifications = await GetNotificationsFromRedis(userId);
        return notifications.FirstOrDefault(n => n.Id == notificationId);
    }

    private async Task<List<Notification>> GetNotificationsFromRedis(string userId)
    {
        var key = GetUserKey(userId);
        var json = await _database.StringGetAsync(key);
        
        if (!json.HasValue)
        {
            return new List<Notification>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<Notification>>(json!) ?? new List<Notification>();
        }
        catch
        {
            // If deserialization fails, clear corrupted data and return empty list
            await _database.KeyDeleteAsync(key);
            return new List<Notification>();
        }
    }
}