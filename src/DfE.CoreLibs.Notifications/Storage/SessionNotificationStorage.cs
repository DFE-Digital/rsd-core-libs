using System.Text.Json;
using DfE.CoreLibs.Notifications.Interfaces;
using DfE.CoreLibs.Notifications.Models;
using DfE.CoreLibs.Notifications.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DfE.CoreLibs.Notifications.Storage;

/// <summary>
/// Notification storage implementation using HTTP session
/// </summary>
public class SessionNotificationStorage : INotificationStorage
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NotificationServiceOptions _options;

    /// <summary>
    /// Initializes a new instance of the SessionNotificationStorage
    /// </summary>
    /// <param name="httpContextAccessor">HTTP context accessor</param>
    /// <param name="options">Notification service options</param>
    public SessionNotificationStorage(IHttpContextAccessor httpContextAccessor, IOptions<NotificationServiceOptions> options)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    private ISession Session => _httpContextAccessor.HttpContext?.Session 
        ?? throw new InvalidOperationException("Session is not available");

    /// <summary>
    /// Store a notification in session
    /// </summary>
    /// <param name="notification">Notification to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task StoreNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var notifications = GetNotificationsFromSession();
        
        // Remove existing notifications with same context if specified
        if (!string.IsNullOrEmpty(notification.Context))
        {
            notifications.RemoveAll(n => n.Context == notification.Context);
        }

        notifications.Add(notification);
        
        // Keep only the latest notifications to prevent session bloat
        if (notifications.Count > _options.MaxNotificationsPerUser)
        {
            notifications = notifications.OrderByDescending(n => n.CreatedAt)
                .Take(_options.MaxNotificationsPerUser)
                .ToList();
        }

        SaveNotificationsToSession(notifications);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get all notifications for a user (userId is ignored for session storage)
    /// </summary>
    /// <param name="userId">User identifier (ignored for session storage)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All notifications in session</returns>
    public Task<IEnumerable<Notification>> GetNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var notifications = GetNotificationsFromSession();
        return Task.FromResult<IEnumerable<Notification>>(notifications);
    }

    /// <summary>
    /// Update an existing notification
    /// </summary>
    /// <param name="notification">Updated notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task UpdateNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var notifications = GetNotificationsFromSession();
        var index = notifications.FindIndex(n => n.Id == notification.Id);
        
        if (index >= 0)
        {
            notifications[index] = notification;
            SaveNotificationsToSession(notifications);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Remove a specific notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="userId">User identifier (ignored for session storage)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task RemoveNotificationAsync(string notificationId, string userId, CancellationToken cancellationToken = default)
    {
        var notifications = GetNotificationsFromSession();
        notifications.RemoveAll(n => n.Id == notificationId);
        SaveNotificationsToSession(notifications);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Remove notifications by context
    /// </summary>
    /// <param name="context">Context to match</param>
    /// <param name="userId">User identifier (ignored for session storage)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task RemoveNotificationsByContextAsync(string context, string userId, CancellationToken cancellationToken = default)
    {
        var notifications = GetNotificationsFromSession();
        notifications.RemoveAll(n => n.Context == context);
        SaveNotificationsToSession(notifications);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Remove notifications by category
    /// </summary>
    /// <param name="category">Category to match</param>
    /// <param name="userId">User identifier (ignored for session storage)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task RemoveNotificationsByCategoryAsync(string category, string userId, CancellationToken cancellationToken = default)
    {
        var notifications = GetNotificationsFromSession();
        notifications.RemoveAll(n => n.Category == category);
        SaveNotificationsToSession(notifications);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clear all notifications
    /// </summary>
    /// <param name="userId">User identifier (ignored for session storage)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task ClearAllNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        Session.Remove(_options.SessionKey);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get a specific notification by ID
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="userId">User identifier (ignored for session storage)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification if found, null otherwise</returns>
    public Task<Notification?> GetNotificationAsync(string notificationId, string userId, CancellationToken cancellationToken = default)
    {
        var notifications = GetNotificationsFromSession();
        var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
        return Task.FromResult(notification);
    }

    private List<Notification> GetNotificationsFromSession()
    {
        var notificationsJson = Session.GetString(_options.SessionKey);
        if (string.IsNullOrEmpty(notificationsJson))
        {
            return new List<Notification>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<Notification>>(notificationsJson) ?? new List<Notification>();
        }
        catch
        {
            // If deserialization fails, return empty list and clear corrupted data
            Session.Remove(_options.SessionKey);
            return new List<Notification>();
        }
    }

    private void SaveNotificationsToSession(List<Notification> notifications)
    {
        var notificationsJson = JsonSerializer.Serialize(notifications);
        Session.SetString(_options.SessionKey, notificationsJson);
    }
}