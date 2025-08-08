namespace DfE.CoreLibs.Notifications.Contracts.Models;

/// <summary>
/// Types of notifications
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Success notification (green) - indicates successful operations
    /// </summary>
    Success,

    /// <summary>
    /// Error notification (red) - indicates failures or critical issues
    /// </summary>
    Error,

    /// <summary>
    /// Information notification (blue) - provides general information
    /// </summary>
    Info,

    /// <summary>
    /// Warning notification (yellow/amber) - indicates potential issues
    /// </summary>
    Warning
}