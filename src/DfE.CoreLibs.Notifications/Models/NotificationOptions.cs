using System.Diagnostics.CodeAnalysis;

namespace DfE.CoreLibs.Notifications.Models;

/// <summary>
/// Configuration options for notification creation
/// </summary>
[ExcludeFromCodeCoverage]
public class NotificationOptions
{
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
    /// Whether the notification should auto-dismiss after a timeout
    /// </summary>
    public bool AutoDismiss { get; set; } = true;

    /// <summary>
    /// Auto-dismiss timeout in seconds
    /// </summary>
    public int AutoDismissSeconds { get; set; } = 5;

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

    /// <summary>
    /// Whether to remove existing notifications with the same context before adding this one
    /// </summary>
    public bool ReplaceExistingContext { get; set; } = true;
}