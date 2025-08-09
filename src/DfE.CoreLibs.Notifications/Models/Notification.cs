using System.Diagnostics.CodeAnalysis;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.CoreLibs.Notifications.Models;

/// <summary>
/// Represents a user notification that can be displayed in the UI
/// </summary>
[ExcludeFromCodeCoverage]
public class Notification
{
    /// <summary>
    /// Unique identifier for the notification
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The notification message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Type of notification (success, error, info, warning)
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// When the notification was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the notification has been seen/read
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Whether the notification should auto-dismiss after a timeout
    /// </summary>
    public bool AutoDismiss { get; set; } = true;

    /// <summary>
    /// Auto-dismiss timeout in seconds (default 5 seconds)
    /// </summary>
    public int AutoDismissSeconds { get; set; } = 5;

    /// <summary>
    /// Optional context information (e.g., fieldId, uploadId, etc.)
    /// Used for preventing duplicates and contextual operations
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Optional category for grouping notifications
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Optional user identifier for multi-user scenarios
    /// When null, applies to current session/user
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Optional action URL for notifications that link to specific resources
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Optional metadata for extensibility
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Priority level for notification ordering and display
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}