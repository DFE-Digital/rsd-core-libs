using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Notifications.Models;

namespace GovUK.Dfe.CoreLibs.Notifications.Interfaces;

/// <summary>
/// Service for managing user notifications with support for multiple storage providers
/// </summary>
public interface INotificationService
{
    #region Convenience Methods

    /// <summary>
    /// Add a success notification with default settings
    /// </summary>
    /// <param name="message">Notification message</param>
    /// <param name="options">Optional configuration for the notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddSuccessAsync(string message, NotificationOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add an error notification with default settings
    /// </summary>
    /// <param name="message">Notification message</param>
    /// <param name="options">Optional configuration for the notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddErrorAsync(string message, NotificationOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add an info notification with default settings
    /// </summary>
    /// <param name="message">Notification message</param>
    /// <param name="options">Optional configuration for the notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddInfoAsync(string message, NotificationOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a warning notification with default settings
    /// </summary>
    /// <param name="message">Notification message</param>
    /// <param name="options">Optional configuration for the notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddWarningAsync(string message, NotificationOptions? options = null, CancellationToken cancellationToken = default);

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
    Task<Notification> AddNotificationAsync(string message, NotificationType type, NotificationOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all unread notifications for the current context
    /// </summary>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unread notifications</returns>
    Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all notifications (read and unread) for the current context
    /// </summary>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all notifications</returns>
    Task<IEnumerable<Notification>> GetAllNotificationsAsync(string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notifications filtered by category
    /// </summary>
    /// <param name="category">Category to filter by</param>
    /// <param name="unreadOnly">Whether to return only unread notifications</param>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered notifications</returns>
    Task<IEnumerable<Notification>> GetNotificationsByCategoryAsync(string category, bool unreadOnly = false, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark all notifications as read for the current context
    /// </summary>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkAllAsReadAsync(string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a specific notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveNotificationAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all notifications for the current context
    /// </summary>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearAllNotificationsAsync(string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear notifications by category
    /// </summary>
    /// <param name="category">Category to clear</param>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearNotificationsByCategoryAsync(string category, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear notifications by context
    /// </summary>
    /// <param name="context">Context to clear</param>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearNotificationsByContextAsync(string context, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the count of unread notifications
    /// </summary>
    /// <param name="userId">Optional user ID for multi-user scenarios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of unread notifications</returns>
    Task<int> GetUnreadCountAsync(string? userId = null, CancellationToken cancellationToken = default);

    #endregion
}
