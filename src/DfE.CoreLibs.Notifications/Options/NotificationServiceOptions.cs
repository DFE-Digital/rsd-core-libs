namespace DfE.CoreLibs.Notifications.Options;

/// <summary>
/// Configuration options for the notification service
/// </summary>
public class NotificationServiceOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "NotificationService";

    /// <summary>
    /// Storage provider to use (Session, Redis, InMemory, etc.)
    /// </summary>
    public NotificationStorageProvider StorageProvider { get; set; } = NotificationStorageProvider.Session;

    /// <summary>
    /// Maximum number of notifications to keep per user (prevents unlimited growth)
    /// </summary>
    public int MaxNotificationsPerUser { get; set; } = 50;

    /// <summary>
    /// Auto-cleanup interval for old notifications (in minutes)
    /// Set to 0 to disable auto-cleanup
    /// </summary>
    public int AutoCleanupIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum age for notifications before they are auto-cleaned (in hours)
    /// Set to 0 to disable age-based cleanup
    /// </summary>
    public int MaxNotificationAgeHours { get; set; } = 24;

    /// <summary>
    /// Redis connection string (only used when StorageProvider is Redis)
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Redis key prefix for notifications
    /// </summary>
    public string RedisKeyPrefix { get; set; } = "notifications:";

    /// <summary>
    /// Session key for storing notifications (only used when StorageProvider is Session)
    /// </summary>
    public string SessionKey { get; set; } = "UserNotifications";

    /// <summary>
    /// Default auto-dismiss settings for different notification types
    /// </summary>
    public NotificationTypeDefaults TypeDefaults { get; set; } = new();
}

/// <summary>
/// Storage provider options
/// </summary>
public enum NotificationStorageProvider
{
    /// <summary>
    /// Store notifications in HTTP session (default)
    /// </summary>
    Session,

    /// <summary>
    /// Store notifications in Redis cache
    /// </summary>
    Redis,

    /// <summary>
    /// Store notifications in memory (not recommended for production)
    /// </summary>
    InMemory
}

/// <summary>
/// Default settings for different notification types
/// </summary>
public class NotificationTypeDefaults
{
    /// <summary>
    /// Default settings for Success notifications
    /// </summary>
    public NotificationTypeSettings Success { get; set; } = new()
    {
        AutoDismiss = true,
        AutoDismissSeconds = 5
    };

    /// <summary>
    /// Default settings for Error notifications
    /// </summary>
    public NotificationTypeSettings Error { get; set; } = new()
    {
        AutoDismiss = false,
        AutoDismissSeconds = 10
    };

    /// <summary>
    /// Default settings for Info notifications
    /// </summary>
    public NotificationTypeSettings Info { get; set; } = new()
    {
        AutoDismiss = true,
        AutoDismissSeconds = 5
    };

    /// <summary>
    /// Default settings for Warning notifications
    /// </summary>
    public NotificationTypeSettings Warning { get; set; } = new()
    {
        AutoDismiss = true,
        AutoDismissSeconds = 7
    };
}

/// <summary>
/// Settings for a specific notification type
/// </summary>
public class NotificationTypeSettings
{
    /// <summary>
    /// Whether notifications of this type should auto-dismiss by default
    /// </summary>
    public bool AutoDismiss { get; set; } = true;

    /// <summary>
    /// Default auto-dismiss timeout in seconds
    /// </summary>
    public int AutoDismissSeconds { get; set; } = 5;
}