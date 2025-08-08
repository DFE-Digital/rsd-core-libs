namespace DfE.CoreLibs.Notifications.Contracts.Models;

/// <summary>
/// Priority levels for notifications
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Low priority - background information
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority - standard notifications
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority - important notifications that should be prominent
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority - urgent notifications requiring immediate attention
    /// </summary>
    Critical = 3
}