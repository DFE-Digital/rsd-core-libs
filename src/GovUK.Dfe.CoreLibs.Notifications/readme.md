# GovUK.Dfe.CoreLibs.Notifications

A flexible notification system for ASP.NET Core applications that supports multiple storage backends.

## Features

- Multiple storage providers (Redis, Session, In-Memory)
- User-specific notifications
- Auto-dismiss functionality
- Category and context-based organization
- Configurable notification limits and cleanup
- Type-safe notification creation

## Quick Start

### 1. Add the package to your project

```bash
dotnet add package GovUK.Dfe.CoreLibs.Notifications
```

### 2. Configure in appsettings.json

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "NotificationService": {
    "StorageProvider": "Redis",
    "MaxNotificationsPerUser": 50,
    "AutoCleanupIntervalMinutes": 60,
    "MaxNotificationAgeHours": 24,
    "RedisKeyPrefix": "notifications:",
    "SessionKey": "UserNotifications",
    "TypeDefaults": {
      "Success": {
        "AutoDismiss": true,
        "AutoDismissSeconds": 5
      },
      "Error": {
        "AutoDismiss": false,
        "AutoDismissSeconds": 10
      },
      "Info": {
        "AutoDismiss": true,
        "AutoDismissSeconds": 5
      },
      "Warning": {
        "AutoDismiss": true,
        "AutoDismissSeconds": 7
      }
    }
  }
}
```

### 3. Register services in Program.cs

Choose one of the following based on your storage preference:

#### Redis Storage (Recommended for production)
```csharp
// Using configuration from appsettings.json
services.AddNotificationServicesWithRedis(configuration);

// Or with explicit connection string
services.AddNotificationServicesWithRedis("localhost:6379");
```

#### Session Storage (Good for development)
```csharp
// Using configuration from appsettings.json
services.AddNotificationServicesWithSession(configuration);

// Or with explicit configuration
services.AddNotificationServicesWithSession(options =>
{
    options.MaxNotificationsPerUser = 25;
    options.SessionKey = "MyNotifications";
});
```

#### In-Memory Storage (For testing only)
```csharp
// Using configuration from appsettings.json
services.AddNotificationServicesWithInMemory(configuration);

// Or with explicit configuration
services.AddNotificationServicesWithInMemory(options =>
{
    options.MaxNotificationsPerUser = 10;
});
```

### 4. Use in your controllers or services

```csharp
public class HomeController : Controller
{
    private readonly INotificationService _notificationService;

    public HomeController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        // Add a success notification
        await _notificationService.AddSuccessAsync("Welcome to the application!");

        // Add an error notification
        await _notificationService.AddErrorAsync("Something went wrong!");

        // Add a custom notification
        await _notificationService.AddNotificationAsync(
            "Custom message",
            NotificationType.Info,
            new NotificationOptions
            {
                Category = "System",
                Context = "HomePage",
                AutoDismiss = true,
                AutoDismissSeconds = 3
            });

        return View();
    }
}
```

## Configuration Options

### NotificationServiceOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| StorageProvider | NotificationStorageProvider | Session | Storage backend to use |
| MaxNotificationsPerUser | int | 50 | Maximum notifications per user |
| AutoCleanupIntervalMinutes | int | 60 | Auto-cleanup interval (0 = disabled) |
| MaxNotificationAgeHours | int | 24 | Maximum age before cleanup (0 = disabled) |
| RedisConnectionString | string | null | Redis connection string |
| RedisKeyPrefix | string | "notifications:" | Redis key prefix |
| SessionKey | string | "UserNotifications" | Session key for storage |
| TypeDefaults | NotificationTypeDefaults | Defaults | Default settings per notification type |

### Storage Providers

#### Redis
- **Pros**: Scalable, persistent, supports multiple application instances
- **Cons**: Requires Redis server
- **Best for**: Production environments

#### Session
- **Pros**: Simple, no external dependencies
- **Cons**: Not shared between application instances, lost on session expiry
- **Best for**: Development, single-instance applications

#### In-Memory
- **Pros**: Fastest, no external dependencies
- **Cons**: Data lost on application restart, not shared between instances
- **Best for**: Testing only

## API Reference

### INotificationService

#### Convenience Methods
- `AddSuccessAsync(message, options, cancellationToken)`
- `AddErrorAsync(message, options, cancellationToken)`
- `AddInfoAsync(message, options, cancellationToken)`
- `AddWarningAsync(message, options, cancellationToken)`

#### Core Methods
- `AddNotificationAsync(message, type, options, cancellationToken)`
- `GetAllNotificationsAsync(userId, cancellationToken)`
- `GetUnreadNotificationsAsync(userId, cancellationToken)`
- `GetNotificationsByCategoryAsync(category, unreadOnly, userId, cancellationToken)`
- `MarkAsReadAsync(notificationId, cancellationToken)`
- `MarkAllAsReadAsync(userId, cancellationToken)`
- `RemoveNotificationAsync(notificationId, cancellationToken)`
- `ClearAllNotificationsAsync(userId, cancellationToken)`
- `ClearNotificationsByCategoryAsync(category, userId, cancellationToken)`
- `ClearNotificationsByContextAsync(context, userId, cancellationToken)`
- `GetUnreadCountAsync(userId, cancellationToken)`

### NotificationOptions

| Property | Type | Description |
|----------|------|-------------|
| Category | string | Optional category for grouping |
| Context | string | Optional context for replacing similar notifications |
| AutoDismiss | bool | Whether to auto-dismiss |
| AutoDismissSeconds | int | Auto-dismiss timeout in seconds |
| UserId | string | Explicit user ID (optional) |

## Examples

### Custom User Context Provider

```csharp
public class CustomUserContextProvider : IUserContextProvider
{
    public string GetCurrentUserId()
    {
        // Your custom logic to get user ID
        return "custom-user-id";
    }
}

// Register with custom providers
services.AddNotificationServicesWithCustomProviders<RedisNotificationStorage, CustomUserContextProvider>(configuration);
```

### Notification with Category and Context

```csharp
await _notificationService.AddNotificationAsync(
    "Your profile has been updated",
    NotificationType.Success,
    new NotificationOptions
    {
        Category = "Profile",
        Context = "ProfileUpdate",
        AutoDismiss = true,
        AutoDismissSeconds = 5
    });
```

### Batch Operations

```csharp
// Mark all notifications as read
await _notificationService.MarkAllAsReadAsync();

// Clear all notifications in a category
await _notificationService.ClearNotificationsByCategoryAsync("System");

// Get unread count
var unreadCount = await _notificationService.GetUnreadCountAsync();
```

## Migration from Previous Versions

If you were using the old `AddNotificationServices` method, replace it with the specific storage method:

```csharp
choose one:
services.AddNotificationServicesWithRedis(configuration);
services.AddNotificationServicesWithSession(configuration);
services.AddNotificationServicesWithInMemory(configuration);
```

The new methods are more explicit and configure all necessary dependencies automatically.
