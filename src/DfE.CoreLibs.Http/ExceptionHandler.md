# Global Exception Handler Middleware

This middleware provides a standardized way to handle all unhandled exceptions in ASP.NET Core applications. It formats exceptions into consistent JSON responses with unique error IDs for tracking in Application Insights.

## Features

- ✅ **Global Exception Handling**: Catches all unhandled exceptions
- ✅ **Standardized Responses**: Consistent JSON error format
- ✅ **Unique Error IDs**: 6-digit identifiers for tracking (default) with customizable generators
- ✅ **Environment-Aware IDs**: Automatic environment prefixes (D/T/P for Dev/Test/Prod)
- ✅ **Correlation ID Support**: Integrates with existing correlation tracking
- ✅ **Customizable**: Configurable status codes, messages, and behavior
- ✅ **Extensible**: Support for custom exception handlers
- ✅ **Shared Post-Processing**: Common logic across all handlers
- ✅ **Security**: Hides sensitive details in production
- ✅ **Logging**: Automatic exception logging with structured data

## Quick Start

### 1. Basic Usage

```csharp
// Program.cs or Startup.cs
using DfE.CoreLibs.Http.Extensions;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Add the middleware early in the pipeline
app.UseGlobalExceptionHandler();

// Your other middleware and endpoints
app.MapControllers();
app.Run();
```

### 2. With Custom Configuration

```csharp
// Program.cs
app.UseGlobalExceptionHandler(options =>
{
    options.IncludeDetails = builder.Environment.IsDevelopment();
    options.LogExceptions = true;
    options.DefaultErrorMessage = "Something went wrong";
    
    // Ignore specific exception types
    options.IgnoredExceptionTypes.Add(typeof(OperationCanceledException));
});
```

### 3. With Dependency Injection

```csharp
// Program.cs
builder.Services.ConfigureGlobalExceptionHandler(options =>
{
    options.IncludeDetails = builder.Environment.IsDevelopment();
});

var app = builder.Build();
app.UseGlobalExceptionHandler();
```

## Error ID Generation

The middleware provides multiple ways to generate error IDs:

### Default 6-Digit Random IDs

By default, the middleware generates random 6-digit error IDs (e.g., "123456").

### Environment-Aware Error IDs

The middleware supports automatic environment prefixes:

```csharp
// Environment-aware error IDs
app.UseGlobalExceptionHandler(options =>
{
    options.WithEnvironmentAwareErrorIds("Development"); // D-123456
    // or
    options.WithEnvironmentAwareErrorIds("Production"); // P-123456
    // or
    options.WithEnvironmentAwareErrorIds("Test"); // T-123456
});
```

### Available Environment Prefixes

| Environment Name | Prefix | Example |
|------------------|--------|---------|
| Development/Dev | D | `D-123456` |
| Test/Staging | T | `T-123456` |
| Production/Prod | P | `P-123456` |
| UAT | U | `U-123456` |
| QA | Q | `Q-123456` |
| Unknown | X | `X-123456` |

### Custom Error ID Generators

```csharp
// Custom error ID generator
app.UseGlobalExceptionHandler(options =>
{
    options.WithCustomErrorIdGenerator(() => 
        $"ERR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 15));
});

// Built-in generators
app.UseGlobalExceptionHandler(options =>
{
    options.WithTimestampBasedErrorIds(); // YYYYMMDD-HHMMSS-XXXX
    // or
    options.WithGuidBasedErrorIds(); // First 8 chars of GUID
    // or
    options.WithSequentialErrorIds(); // Unix timestamp
});
```

### Environment-Aware Built-in Generators

```csharp
// Environment-aware timestamp-based error IDs
app.UseGlobalExceptionHandler(options =>
{
    options.WithEnvironmentAwareTimestampErrorIds("Development"); // D-20240115-143022-5678
});

// Environment-aware GUID-based error IDs
app.UseGlobalExceptionHandler(options =>
{
    options.WithEnvironmentAwareGuidErrorIds("Production"); // P-a1b2c3d4
});

// Environment-aware sequential error IDs
app.UseGlobalExceptionHandler(options =>
{
    options.WithEnvironmentAwareSequentialErrorIds("Test"); // T-1705321822123
});
```

### Available Error ID Generators

| Generator | Format | Example |
|-----------|--------|---------|
| Default | 6-digit random | `123456` |
| Environment-aware Default | `{Prefix}-{6-digit}` | `D-123456` |
| Timestamp-based | `YYYYMMDD-HHMMSS-XXXX` | `20240115-143022-5678` |
| Environment-aware Timestamp | `{Prefix}-YYYYMMDD-HHMMSS-XXXX` | `D-20240115-143022-5678` |
| GUID-based | First 8 chars of GUID | `a1b2c3d4` |
| Environment-aware GUID | `{Prefix}-{8-char-GUID}` | `P-a1b2c3d4` |
| Sequential | Unix timestamp | `1705321822123` |
| Environment-aware Sequential | `{Prefix}-{timestamp}` | `T-1705321822123` |

## Shared Post-Processing

You can configure shared post-processing logic that runs after any handler processes an exception:

```csharp
app.UseGlobalExceptionHandler(options =>
{
    options.WithSharedPostProcessing((exception, response) =>
    {
        // Increment error counter
        IncrementErrorCounter(response.StatusCode);

        // Send notification for critical errors
        if (response.StatusCode >= 500)
        {
            SendCriticalErrorNotification(response);
        }

        // Add custom context
        response.Context = new Dictionary<string, object>
        {
            ["processedAt"] = DateTime.UtcNow,
            ["environment"] = "production"
        };
    });
});
```

### Common Post-Processing Use Cases

- **Metrics Tracking**: Increment error counters
- **Notifications**: Send alerts for critical errors
- **External Logging**: Log to external systems
- **Context Enrichment**: Add environment info, timestamps
- **Monitoring**: Send data to monitoring systems

## Custom Exception Handlers

The middleware supports custom exception handlers that allow services to register their own exception handling logic.

### 1. Create Custom Exceptions

```csharp
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
```

### 2. Implement Custom Exception Handler

```csharp
using DfE.CoreLibs.Http.Interfaces;

public class BusinessRuleExceptionHandler : ICustomExceptionHandler
{
    public int Priority => 10; // Higher priority than default handler

    public bool CanHandle(Type exceptionType)
    {
        return exceptionType == typeof(BusinessRuleException);
    }

    public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
    {
        return new ExceptionResponse
        {
            StatusCode = 422,
            Message = $"Business rule violation: {exception.Message}",
            ExceptionType = "BusinessRuleException",
            Context = context
        };
    }
}
```

### 3. Register Custom Handlers

```csharp
// Program.cs
builder.Services.AddCustomExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddCustomExceptionHandler<ValidationExceptionHandler>();

// Or register multiple handlers at once
builder.Services.AddCustomExceptionHandlers(
    new BusinessRuleExceptionHandler(),
    new ValidationExceptionHandler()
);

var app = builder.Build();
app.UseGlobalExceptionHandler();
```

### 4. Priority System

Handlers are executed in priority order (lower numbers = higher priority):

- **Priority 1-50**: High priority custom handlers
- **Priority 51-99**: Medium priority custom handlers  
- **Priority 100**: Default handler (built-in .NET exceptions)

```csharp
public class HighPriorityHandler : ICustomExceptionHandler
{
    public int Priority => 5; // Very high priority
    // ...
}

public class MediumPriorityHandler : ICustomExceptionHandler
{
    public int Priority => 50; // Medium priority
    // ...
}
```

### 5. Context-Aware Handlers

Handlers can use context information for more specific error messages:

```csharp
public class ContextAwareHandler : ICustomExceptionHandler
{
    public int Priority => 10;

    public bool CanHandle(Type exceptionType)
    {
        return exceptionType == typeof(InvalidOperationException);
    }

    public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
    {
        var message = context?.TryGetValue("operation", out var operation) == true
            ? $"Operation '{operation}' failed: {exception.Message}"
            : $"Invalid operation: {exception.Message}";

        return new ExceptionResponse
        {
            StatusCode = 400,
            Message = message,
            ExceptionType = "InvalidOperationException",
            Context = context
        };
    }
}
```

## Response Format

All exceptions are formatted into a consistent JSON response:

```json
{
  "errorId": "D-123456",
  "statusCode": 400,
  "message": "Invalid request: Required parameter is missing",
  "exceptionType": "ArgumentNullException",
  "timestamp": "2024-01-15T10:30:00.000Z",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "details": null,
  "context": {
    "processedAt": "2024-01-15T10:30:00.000Z",
    "environment": "production"
  }
}
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `IncludeDetails` | bool | false | Include full exception details (development only) |
| `LogExceptions` | bool | true | Automatically log exceptions |
| `CustomHandlers` | List<ICustomExceptionHandler> | Empty | Custom exception handlers |
| `IgnoredExceptionTypes` | HashSet | Empty | Exception types to ignore |
| `DefaultErrorMessage` | string | "An unexpected error occurred" | Default error message |
| `IncludeCorrelationId` | bool | true | Include correlation ID in responses |
| `ErrorIdGenerator` | Func<string> | 6-digit random | Custom error ID generator |
| `SharedPostProcessingAction` | Action<Exception, ExceptionResponse> | null | Shared post-processing logic |

## Built-in Exception Mappings

| Exception Type | Status Code | Message |
|----------------|-------------|---------|
| `ArgumentNullException` | 400 | "Invalid request: Required parameter is missing" |
| `ArgumentException` | 400 | "Invalid request: {message}" |
| `InvalidOperationException` | 400 | "Invalid operation: {message}" |
| `UnauthorizedAccessException` | 401 | "Unauthorized access" |
| `NotImplementedException` | 501 | "Feature not implemented" |
| `FileNotFoundException` | 404 | "Resource not found" |
| `DirectoryNotFoundException` | 404 | "Directory not found" |
| `TimeoutException` | 408 | "Request timeout" |
| All others | 500 | "An unexpected error occurred" |

## Advanced Usage Examples

### Environment-Aware Configuration

```csharp
var environmentName = app.Environment.EnvironmentName; // "Development", "Production", etc.

var options = new ExceptionHandlerOptions
{
    IncludeDetails = app.Environment.IsDevelopment(),
    LogExceptions = true,
    DefaultErrorMessage = app.Environment.IsProduction() ? "Something went wrong" : "An error occurred"
}
.WithEnvironmentAwareErrorIds(environmentName) // D-123456, P-123456, etc.
.WithSharedPostProcessing((exception, response) =>
{
    // Environment-specific post-processing
    if (app.Environment.IsDevelopment())
    {
        Console.WriteLine($"DEV ERROR: {response.ErrorId} - {response.Message}");
    }
    else
    {
        SendToMonitoringSystem(response);
    }
});

app.UseGlobalExceptionHandler(options);
```

### Environment-Specific Error ID Strategies

```csharp
var environmentName = app.Environment.EnvironmentName;

var options = new ExceptionHandlerOptions
{
    IncludeDetails = app.Environment.IsDevelopment(),
    LogExceptions = true
};

// Different strategies for different environments
switch (environmentName)
{
    case "Development":
        options.WithEnvironmentAwareErrorIds(environmentName); // D-123456
        break;
    case "Test":
        options.WithEnvironmentAwareTimestampErrorIds(environmentName); // T-20240115-143022-5678
        break;
    case "Production":
        options.WithEnvironmentAwareGuidErrorIds(environmentName); // P-a1b2c3d4
        break;
    default:
        options.WithEnvironmentAwareSequentialErrorIds(environmentName); // X-1705321822123
        break;
}

app.UseGlobalExceptionHandler(options);
```

### Combined Custom Handlers with Environment-Aware Error IDs

```csharp
var options = new ExceptionHandlerOptions
{
    IncludeDetails = false,
    LogExceptions = true,
    CustomHandlers = new List<ICustomExceptionHandler>
    {
        new BusinessRuleExceptionHandler(),
        new ValidationExceptionHandler()
    }
}
.WithEnvironmentAwareTimestampErrorIds(app.Environment.EnvironmentName) // D-20240115-143022-5678
.WithSharedPostProcessing((exception, response) =>
{
    // Common post-processing logic
    TrackErrorMetrics(response);
    NotifyTeamIfNeeded(response);
});

app.UseGlobalExceptionHandler(options);
```

## Service Integration Examples

### File Storage Service Integration

```csharp
// In your FileStorage service
public class FileStorageExceptionHandler : ICustomExceptionHandler
{
    public int Priority => 15;

    public bool CanHandle(Type exceptionType)
    {
        return exceptionType == typeof(FileStorageException) ||
               exceptionType == typeof(FileNotFoundException);
    }

    public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
    {
        return exception switch
        {
            FileStorageException => new ExceptionResponse
            {
                StatusCode = 500,
                Message = "File storage operation failed",
                ExceptionType = "FileStorageException",
                Context = context
            },
            FileNotFoundException => new ExceptionResponse
            {
                StatusCode = 404,
                Message = "File not found",
                ExceptionType = "FileNotFoundException",
                Context = context
            },
            _ => new ExceptionResponse
            {
                StatusCode = 500,
                Message = "File operation failed",
                ExceptionType = exception.GetType().Name,
                Context = context
            }
        };
    }
}

// Register in your service
builder.Services.AddCustomExceptionHandler<FileStorageExceptionHandler>();
```

### Security Service Integration

```csharp
// In your Security service
public class SecurityExceptionHandler : ICustomExceptionHandler
{
    public int Priority => 5; // Very high priority for security exceptions

    public bool CanHandle(Type exceptionType)
    {
        return exceptionType == typeof(UnauthorizedAccessException) ||
               exceptionType == typeof(SecurityException);
    }

    public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
    {
        return exception switch
        {
            UnauthorizedAccessException => new ExceptionResponse
            {
                StatusCode = 401,
                Message = "Access denied",
                ExceptionType = "UnauthorizedAccessException",
                Context = context
            },
            SecurityException => new ExceptionResponse
            {
                StatusCode = 403,
                Message = "Security violation",
                ExceptionType = "SecurityException",
                Context = context
            },
            _ => new ExceptionResponse
            {
                StatusCode = 401,
                Message = "Authentication required",
                ExceptionType = exception.GetType().Name,
                Context = context
            }
        };
    }
}
```

## Integration with Application Insights

The middleware automatically includes error IDs and correlation IDs in logs, making it easy to track issues:

```csharp
// In your Application Insights queries
traces
| where customDimensions.ErrorId startswith "D-" // Development errors
| project timestamp, message, customDimensions

// Or by correlation ID
traces
| where customDimensions.CorrelationId == "550e8400-e29b-41d4-a716-446655440000"
| project timestamp, message, customDimensions

// Environment-specific queries
traces
| where customDimensions.ErrorId startswith "P-" // Production errors
| project timestamp, message, customDimensions
```

## Best Practices

### 1. Order in Pipeline

Place the middleware early in your pipeline, but after authentication/authorization:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseGlobalExceptionHandler(); // After auth, before endpoints
app.MapControllers();
```

### 2. Custom Exception Design

Create custom exceptions that work well with the middleware:

```csharp
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
```

### 3. Handler Priority Strategy

- **Priority 1-10**: Critical system exceptions (security, authentication)
- **Priority 11-30**: Business logic exceptions (validation, business rules)
- **Priority 31-50**: Service-specific exceptions (file storage, database)
- **Priority 51-99**: General application exceptions
- **Priority 100**: Default handler (built-in .NET exceptions)

### 4. Error ID Strategy

- **Development**: Use environment-aware default IDs (e.g., `D-123456`)
- **Test**: Use environment-aware timestamp IDs (e.g., `T-20240115-143022-5678`)
- **Production**: Use environment-aware GUID IDs (e.g., `P-a1b2c3d4`)
- **High-volume**: Use environment-aware sequential IDs (e.g., `P-1705321822123`)
- **Security-sensitive**: Use GUID-based IDs for unpredictability

### 5. Environment-Aware Strategy

- **Development**: Use simple IDs for easy debugging (`D-123456`)
- **Test**: Use timestamp IDs for chronological tracking (`T-20240115-143022-5678`)
- **Production**: Use GUID IDs for security and uniqueness (`P-a1b2c3d4`)
- **UAT/QA**: Use appropriate prefixes (`U-123456`, `Q-123456`)

### 6. Post-Processing Strategy

- **Metrics**: Always track error counts and types
- **Notifications**: Only for critical errors (500+ status codes)
- **Logging**: Keep it lightweight to avoid performance impact
- **Context**: Add useful debugging information
- **Environment Tracking**: Use environment prefixes in logs and metrics

### 7. Testing

Test your exception handling:

```csharp
[Fact]
public async Task Should_Return_Formatted_Error_Response()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/test/throw-exception");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var content = await response.Content.ReadAsStringAsync();
    var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(content);
    
    errorResponse.Should().NotBeNull();
    errorResponse!.ErrorId.Should().MatchRegex(@"^[DTPUQX]-\d{6}$"); // Environment-aware pattern
    errorResponse.StatusCode.Should().Be(400);
}
```

## Security Considerations

- **Never include details in production**: Set `IncludeDetails = false` in production
- **Sanitize error messages**: Ensure custom messages don't leak sensitive information
- **Log appropriately**: Use structured logging for better analysis
- **Rate limiting**: Consider rate limiting for error endpoints to prevent abuse
- **Error ID security**: Use unpredictable IDs in security-sensitive applications
- **Environment isolation**: Use different error ID strategies per environment

## Migration from Existing Error Handling

If you have existing error handling, you can gradually migrate:

```csharp
// 1. Add the middleware alongside existing handlers
app.UseGlobalExceptionHandler();
app.UseExceptionHandler("/Error"); // Keep existing for now

// 2. Gradually remove try-catch blocks from controllers
// 3. Remove the old exception handler
// 4. Update client code to handle the new response format
```

## Legacy Support

The middleware maintains backward compatibility with the old dictionary-based approach:

```csharp
// Legacy approach (still supported but deprecated)
app.UseGlobalExceptionHandler(options =>
{
    options.ExceptionStatusCodes[typeof(ValidationException)] = 422;
    options.ExceptionMessages[typeof(ValidationException)] = "Validation failed";
});

// Recommended approach (new)
builder.Services.AddCustomExceptionHandler<ValidationExceptionHandler>();
``` 