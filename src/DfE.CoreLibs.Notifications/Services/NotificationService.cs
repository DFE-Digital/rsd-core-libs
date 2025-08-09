using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Notifications.Interfaces;
using DfE.CoreLibs.Notifications.Models;
using DfE.CoreLibs.Notifications.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DfE.CoreLibs.Notifications.Services;

/// <summary>
/// Default implementation of the notification service
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationStorage _storage;
    private readonly IUserContextProvider _userContextProvider;
    private readonly NotificationServiceOptions _options;
    private readonly ILogger<NotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the NotificationService
    /// </summary>
    /// <param name="storage">Notification storage provider</param>
    /// <param name="userContextProvider">User context provider</param>
    /// <param name="options">Service options</param>
    /// <param name="logger">Logger</param>
    public NotificationService(
        INotificationStorage storage,
        IUserContextProvider userContextProvider,
        IOptions<NotificationServiceOptions> options,
        ILogger<NotificationService> logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _userContextProvider = userContextProvider ?? throw new ArgumentNullException(nameof(userContextProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Convenience Methods

    /// <summary>
    /// Add a success notification with default settings
    /// </summary>
    /// <param name="message">Notification message</param>
    /// <param name="options">Optional configuration for the notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task AddSuccessAsync(string message, NotificationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var mergedOptions = MergeWithDefaults(options, _options.TypeDefaults.Success);
        await AddNotificationAsync(message, NotificationType.Success, mergedOptions, cancellationToken);
    }

    /// <summary>
    /// Add an error notification with default settings
    /// </summary>
    /// <param name="message">Notification message</param>
    /// <param name="options">Optional configuration for the notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task AddErrorAsync(string message, NotificationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var mergedOptions = MergeWithDefaults(options, _options.TypeDefaults.Error);
        await AddNotificationAsync(message, NotificationType.Error, mergedOptions, cancellationToken);
    }

    /// <summary>
    /// Add an info notification with default settings
    /// </summary>
    /// <param name="message">Notification message</param>
    /// <param name="options">Optional configuration for the notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task AddInfoAsync(string message, NotificationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var mergedOptions = MergeWithDefaults(options, _options.TypeDefaults.Info);
        await AddNotificationAsync(message, NotificationType.Info, mergedOptions, cancellationToken);
    }

    /// <summary>
    /// Add a warning notification with default settings
    /// </summary>
    /// <param name="message">Notification message</param>
    /// <param name="options">Optional configuration for the notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task AddWarningAsync(string message, NotificationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var mergedOptions = MergeWithDefaults(options, _options.TypeDefaults.Warning);
        await AddNotificationAsync(message, NotificationType.Warning, mergedOptions, cancellationToken);
    }

    #endregion

    #region Core Methods

    /// <summary>
    /// Add a notification with full control over all properties
    /// </summary>
    /// <param name="message">Notification message</param>
    /// <param name="type">Type of notification</param>
    /// <param name="options">Optional configuration for the notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created notification</returns>
    public async Task<Notification> AddNotificationAsync(string message, NotificationType type, NotificationOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or empty", nameof(message));

        try
        {
            var userId = GetUserId(options?.UserId);
            
            var notification = new Notification
            {
                Message = message,
                Type = type,
                UserId = userId,
                Context = options?.Context,
                Category = options?.Category,
                AutoDismiss = options?.AutoDismiss ?? true,
                AutoDismissSeconds = options?.AutoDismissSeconds ?? 5,
                ActionUrl = options?.ActionUrl,
                Metadata = options?.Metadata,
                Priority = options?.Priority ?? NotificationPriority.Normal
            };

            await _storage.StoreNotificationAsync(notification, cancellationToken);
            
            _logger.LogDebug("Added {Type} notification for user {UserId}: {Message}", type, userId, message);
            
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add notification: {Message}", message);
            throw;
        }
    }

    /// <summary>
    /// Get all unread notifications for the current context
    /// </summary>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unread notifications</returns>
    public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedUserId = GetUserId(userId);
            var notifications = await _storage.GetNotificationsAsync(resolvedUserId, cancellationToken);
            
            return notifications
                .Where(n => !n.IsRead)
                .OrderByDescending(n => n.Priority)
                .ThenByDescending(n => n.CreatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unread notifications for user {UserId}", userId);
            return Enumerable.Empty<Notification>();
        }
    }

    /// <summary>
    /// Get all notifications (read and unread) for the current context
    /// </summary>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all notifications</returns>
    public async Task<IEnumerable<Notification>> GetAllNotificationsAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedUserId = GetUserId(userId);
            var notifications = await _storage.GetNotificationsAsync(resolvedUserId, cancellationToken);
            
            return notifications
                .OrderByDescending(n => n.Priority)
                .ThenByDescending(n => n.CreatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all notifications for user {UserId}", userId);
            return Enumerable.Empty<Notification>();
        }
    }

    /// <summary>
    /// Get notifications filtered by category
    /// </summary>
    /// <param name="category">Category to filter by</param>
    /// <param name="unreadOnly">Whether to return only unread notifications</param>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered notifications</returns>
    public async Task<IEnumerable<Notification>> GetNotificationsByCategoryAsync(string category, bool unreadOnly = false, string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedUserId = GetUserId(userId);
            var notifications = await _storage.GetNotificationsAsync(resolvedUserId, cancellationToken);
            
            var filtered = notifications.Where(n => n.Category == category);
            
            if (unreadOnly)
            {
                filtered = filtered.Where(n => !n.IsRead);
            }
            
            return filtered
                .OrderByDescending(n => n.Priority)
                .ThenByDescending(n => n.CreatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications by category {Category} for user {UserId}", category, userId);
            return Enumerable.Empty<Notification>();
        }
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            var notification = await _storage.GetNotificationAsync(notificationId, userId, cancellationToken);
            
            if (notification != null)
            {
                notification.IsRead = true;
                await _storage.UpdateNotificationAsync(notification, cancellationToken);
                
                _logger.LogDebug("Marked notification {NotificationId} as read for user {UserId}", notificationId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as read", notificationId);
            throw;
        }
    }

    /// <summary>
    /// Mark all notifications as read for the current context
    /// </summary>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task MarkAllAsReadAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedUserId = GetUserId(userId);
            var notifications = await _storage.GetNotificationsAsync(resolvedUserId, cancellationToken);
            
            foreach (var notification in notifications.Where(n => !n.IsRead))
            {
                notification.IsRead = true;
                await _storage.UpdateNotificationAsync(notification, cancellationToken);
            }
            
            _logger.LogDebug("Marked all notifications as read for user {UserId}", resolvedUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Remove a specific notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemoveNotificationAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            await _storage.RemoveNotificationAsync(notificationId, userId, cancellationToken);
            
            _logger.LogDebug("Removed notification {NotificationId} for user {UserId}", notificationId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove notification {NotificationId}", notificationId);
            throw;
        }
    }

    /// <summary>
    /// Clear all notifications for the current context
    /// </summary>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ClearAllNotificationsAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedUserId = GetUserId(userId);
            await _storage.ClearAllNotificationsAsync(resolvedUserId, cancellationToken);
            
            _logger.LogDebug("Cleared all notifications for user {UserId}", resolvedUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear all notifications for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Clear notifications by category
    /// </summary>
    /// <param name="category">Category to clear</param>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ClearNotificationsByCategoryAsync(string category, string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedUserId = GetUserId(userId);
            await _storage.RemoveNotificationsByCategoryAsync(category, resolvedUserId, cancellationToken);
            
            _logger.LogDebug("Cleared notifications by category {Category} for user {UserId}", category, resolvedUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear notifications by category {Category} for user {UserId}", category, userId);
            throw;
        }
    }

    /// <summary>
    /// Clear notifications by context
    /// </summary>
    /// <param name="context">Context to clear</param>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ClearNotificationsByContextAsync(string context, string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedUserId = GetUserId(userId);
            await _storage.RemoveNotificationsByContextAsync(context, resolvedUserId, cancellationToken);
            
            _logger.LogDebug("Cleared notifications by context {Context} for user {UserId}", context, resolvedUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear notifications by context {Context} for user {UserId}", context, userId);
            throw;
        }
    }

    /// <summary>
    /// Get the count of unread notifications
    /// </summary>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of unread notifications</returns>
    public async Task<int> GetUnreadCountAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedUserId = GetUserId(userId);
            var notifications = await _storage.GetNotificationsAsync(resolvedUserId, cancellationToken);
            
            return notifications.Count(n => !n.IsRead);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unread count for user {UserId}", userId);
            return 0;
        }
    }

    #endregion

    #region Private Methods

    private string GetUserId(string? explicitUserId = null)
    {
        if (!string.IsNullOrEmpty(explicitUserId))
            return explicitUserId;

        return _userContextProvider.GetCurrentUserId();
    }

    private static NotificationOptions MergeWithDefaults(NotificationOptions? options, NotificationTypeSettings defaults)
    {
        if (options == null)
        {
            return new NotificationOptions
            {
                AutoDismiss = defaults.AutoDismiss,
                AutoDismissSeconds = defaults.AutoDismissSeconds
            };
        }

        // Only apply defaults if not explicitly set
        return new NotificationOptions
        {
            Context = options.Context,
            Category = options.Category,
            AutoDismiss = options.AutoDismiss, // Keep user's setting
            AutoDismissSeconds = options.AutoDismissSeconds, // Keep user's setting
            UserId = options.UserId,
            ActionUrl = options.ActionUrl,
            Metadata = options.Metadata,
            Priority = options.Priority,
            ReplaceExistingContext = options.ReplaceExistingContext
        };
    }

    #endregion
}