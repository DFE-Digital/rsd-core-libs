using DfE.CoreLibs.Notifications.Contracts.Models;

namespace DfE.CoreLibs.Notifications.Contracts.Interfaces;

/// <summary>
/// Abstraction for notification storage providers (session, Redis, database, etc.)
/// </summary>
public interface INotificationStorage
{
    /// <summary>
    /// Store a notification
    /// </summary>
    /// <param name="notification">Notification to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StoreNotificationAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all notifications for a user/context
    /// </summary>
    /// <param name="userId">User identifier (can be session ID, user ID, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All notifications for the user</returns>
    Task<IEnumerable<Notification>> GetNotificationsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing notification
    /// </summary>
    /// <param name="notification">Updated notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateNotificationAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a specific notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveNotificationAsync(string notificationId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove notifications by context
    /// </summary>
    /// <param name="context">Context to match</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveNotificationsByContextAsync(string context, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove notifications by category
    /// </summary>
    /// <param name="category">Category to match</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveNotificationsByCategoryAsync(string category, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all notifications for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearAllNotificationsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific notification by ID
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification if found, null otherwise</returns>
    Task<Notification?> GetNotificationAsync(string notificationId, string userId, CancellationToken cancellationToken = default);
}