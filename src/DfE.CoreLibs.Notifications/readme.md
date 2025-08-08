# DfE.CoreLibs.Notifications

A flexible notification service library supporting multiple storage providers including session, Redis, in-memory, and database storage.

## Features

- **Multiple Storage Providers**: Session (default), Redis, In-Memory, or custom implementations
- **Flexible Configuration**: Easy setup with dependency injection and configuration
- **Type-safe Notifications**: Strongly typed notification models with enums
- **Context Management**: Support for user-scoped notifications and context deduplication
- **Priority System**: Notification prioritization for better UX
- **Auto-dismiss**: Configurable auto-dismiss functionality
- **Async/Await Support**: Full async API with cancellation token support
- **Logging Integration**: Built-in logging for debugging and monitoring

## Quick Start

### 1. Install Package

```bash
dotnet add package DfE.CoreLibs.Notifications
```

### 2. Configure Services (Session Storage - Default)

```csharp
// Program.cs or Startup.cs
services.AddSession(); // Required for session storage
services.AddNotificationServices();
```

### 3. Use in Controllers/Services

```csharp
public class HomeController : Controller
{
    private readonly INotificationService _notificationService;
    
    public HomeController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
    public async Task<IActionResult> Upload()
    {
        try
        {
            // Your upload logic here...
            
            await _notificationService.AddSuccessAsync("File uploaded successfully!");
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            await _notificationService.AddErrorAsync($"Upload failed: {ex.Message}");
            return View();
        }
    }
    
    public async Task<IActionResult> GetNotifications()
    {
        var notifications = await _notificationService.GetUnreadNotificationsAsync();
        return Json(notifications);
    }
}
```

## Configuration Options

### appsettings.json

```json
{
  "NotificationService": {
    "StorageProvider": "Session", // Session, Redis, InMemory
    "MaxNotificationsPerUser": 50,
    "AutoCleanupIntervalMinutes": 60,
    "MaxNotificationAgeHours": 24,
    "SessionKey": "UserNotifications",
    "RedisConnectionString": "localhost:6379",
    "RedisKeyPrefix": "notifications:",
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

## Storage Providers

### Session Storage (Default)

```csharp
services.AddSession();
services.AddNotificationServices();
```

### Redis Storage

```csharp
services.AddNotificationServicesWithRedis("localhost:6379");
```

### In-Memory Storage

```csharp
services.AddNotificationServicesWithInMemory();
```

### Custom Storage

```csharp
services.AddNotificationServicesWithCustomProviders<MyCustomStorage, MyCustomContextProvider>();
```

## Advanced Usage

### Custom Notification Options

```csharp
var options = new NotificationOptions
{
    Context = "file-upload-123", // Prevents duplicates
    Category = "uploads",
    AutoDismiss = false,
    Priority = NotificationPriority.High,
    ActionUrl = "/files/123",
    Metadata = new Dictionary<string, object>
    {
        ["fileName"] = "document.pdf",
        ["fileSize"] = 1024000
    }
};

await _notificationService.AddSuccessAsync("File uploaded", options);
```

### Filtering and Management

```csharp
// Get notifications by category
var uploadNotifications = await _notificationService.GetNotificationsByCategoryAsync("uploads");

// Clear specific categories
await _notificationService.ClearNotificationsByCategoryAsync("uploads");

// Clear by context (useful for preventing duplicates)
await _notificationService.ClearNotificationsByContextAsync("file-upload-123");

// Mark all as read
await _notificationService.MarkAllAsReadAsync();

// Get unread count for badges
var count = await _notificationService.GetUnreadCountAsync();
```

## Frontend Integration

### JavaScript Example

```javascript
// Get notifications
async function loadNotifications() {
    const response = await fetch('/api/notifications');
    const notifications = await response.json();
    
    notifications.forEach(notification => {
        showNotification(notification);
        
        if (notification.autoDismiss) {
            setTimeout(() => {
                dismissNotification(notification.id);
            }, notification.autoDismissSeconds * 1000);
        }
    });
}

// Mark as read
async function markAsRead(notificationId) {
    await fetch(`/api/notifications/${notificationId}/read`, { method: 'POST' });
}
```

### API Controller Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    
    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var notifications = await _notificationService.GetUnreadNotificationsAsync();
        return Ok(notifications);
    }
    
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return Ok();
    }
    
    [HttpGet("count")]
    public async Task<IActionResult> GetCount()
    {
        var count = await _notificationService.GetUnreadCountAsync();
        return Ok(new { count });
    }
}
```

## Best Practices

1. **Use Context**: Always provide context for related operations to prevent duplicates
2. **Choose Appropriate Storage**: Use Session for simple scenarios, Redis for distributed apps
3. **Set Reasonable Limits**: Configure `MaxNotificationsPerUser` to prevent storage bloat
4. **Handle Errors Gracefully**: The service includes error handling but always wrap in try-catch
5. **Use Categories**: Group related notifications for better management
6. **Consider Priority**: Use priority levels for important notifications

## Migration from Existing Code

If you have existing notification code similar to the example you provided:

1. Replace direct session access with `INotificationService`
2. Use the async methods (`AddSuccessAsync`, etc.)
3. Configure the service with `AddNotificationServices()`
4. Update frontend code to handle the new JSON structure

The service maintains backward compatibility with your existing notification structure while adding flexibility and async support.

## Important Setup Note

**When using session storage (the default), you MUST register `IHttpContextAccessor` in your application:**

```csharp
// In Program.cs or Startup.cs
services.AddHttpContextAccessor(); // Required for session-based notifications
services.AddSession(); // Required for session storage
services.AddNotificationServices();
```

This is required because the notification service needs access to the HTTP session to store and retrieve notifications.