# GovUK.Dfe.CoreLibs.AsyncProcessing

This library provides a high-performance, channel-based framework for implementing asynchronous background task processing in .NET applications. Built on `System.Threading.Channels`, it offers a robust and scalable solution for enqueueing and executing tasks asynchronously with configurable concurrency, capacity, and cancellation support.

## Key Features

- **Channel-Based Architecture**: Uses `System.Threading.Channels` for efficient, thread-safe task queuing
- **Configurable Concurrency**: Support for both sequential and parallel task processing
- **Bounded/Unbounded Channels**: Choose between memory-efficient bounded channels or high-throughput unbounded channels
- **Graceful Shutdown**: Properly handles application shutdown with cancellation token support
- **Flexible Configuration**: Customizable options for channel capacity, worker count, and behavior
- **No External Dependencies**: Minimal footprint with only essential .NET dependencies

## Installation

To install the GovUK.Dfe.CoreLibs.AsyncProcessing Library, use the following command in your .NET project:

```sh
dotnet add package GovUK.Dfe.CoreLibs.AsyncProcessing
```

## Basic Usage

### 1. Service Registration

Register the background service in your `Program.cs` or `Startup.cs`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Basic registration with default settings (sequential processing)
    services.AddBackgroundService();
}
```

### 2. Enqueue Tasks

Inject `IBackgroundServiceFactory` into your services and enqueue background tasks:

```csharp
public class ReportService
{
    private readonly IBackgroundServiceFactory _backgroundServiceFactory;

    public ReportService(IBackgroundServiceFactory backgroundServiceFactory)
    {
        _backgroundServiceFactory = backgroundServiceFactory;
    }

    public async Task<bool> GenerateReportAsync(string reportId)
    {
        // Enqueue a background task and get a Task<TResult> back
        var resultTask = _backgroundServiceFactory.EnqueueTask(async (cancellationToken) =>
        {
            // Your background work here
            await Task.Delay(5000, cancellationToken); // Simulate long-running operation
            var report = await GenerateReportDataAsync(reportId, cancellationToken);
            await SaveReportAsync(report, cancellationToken);
            return true;
        });

        // You can await the result if needed, or return immediately
        return await resultTask;
    }
}
```

## Advanced Configuration

### Parallel Processing

Enable parallel task processing with multiple concurrent workers:

```csharp
services.AddBackgroundService(options =>
{
    options.MaxConcurrentWorkers = 4;  // Process up to 4 tasks concurrently
    options.UseGlobalStoppingToken = true;  // Cancel tasks on app shutdown
});

// Or use the convenience method:
services.AddBackgroundServiceWithParallelism(
    maxConcurrentWorkers: 4,
    channelCapacity: 100  // Optional: limit queue size
);
```

### Bounded Channels

Configure bounded channels to prevent memory issues with heavy task loads:

```csharp
services.AddBackgroundService(options =>
{
    options.ChannelCapacity = 1000;  // Limit queue to 1000 tasks
    options.ChannelFullMode = ChannelFullMode.Wait;  // Block when full
    // Other options: ChannelFullMode.DropOldest, ChannelFullMode.ThrowException
});
```

### Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `MaxConcurrentWorkers` | `1` | Number of concurrent workers processing tasks |
| `ChannelCapacity` | `int.MaxValue` | Maximum channel capacity (unbounded if max value) |
| `ChannelFullMode` | `Wait` | Behavior when channel is full: `Wait`, `DropOldest`, `ThrowException` |
| `UseGlobalStoppingToken` | `false` | Pass app shutdown token to tasks for graceful cancellation |
| `EnableDetailedLogging` | `false` | Enable verbose logging for diagnostics |

## Task Cancellation

You can pass a cancellation token to cancel specific tasks:

```csharp
var cts = new CancellationTokenSource();

var taskResult = _backgroundServiceFactory.EnqueueTask(async (token) =>
{
    // The token will be cancelled if either:
    // 1. The caller cancels cts
    // 2. The app is shutting down (if UseGlobalStoppingToken = true)
    
    await LongRunningOperationAsync(token);
    return "Success";
}, cts.Token);

// Cancel this specific task
cts.Cancel();
```

## Use Cases

- **Report Generation**: Offload long-running report generation to background tasks
- **Data Processing**: Process large datasets asynchronously without blocking API requests
- **Email Sending**: Queue email sending operations for batch processing
- **File Processing**: Handle file uploads, conversions, and transformations asynchronously
- **Batch Operations**: Execute batch updates or maintenance tasks in the background

## Event Handling / Message Broker Integration

This library focuses solely on task execution and does not include built-in event handling or message broker integration. This design allows you to integrate your preferred messaging solution (RabbitMQ, Azure Service Bus, Kafka, etc.) by wrapping the task execution with your own event publishing logic:

```csharp
public class ReportServiceWithEvents
{
    private readonly IBackgroundServiceFactory _backgroundServiceFactory;
    private readonly IMessagePublisher _messagePublisher;  // Your choice of message broker

    public async Task GenerateReportAsync(string reportId)
    {
        var taskResult = _backgroundServiceFactory.EnqueueTask(async (cancellationToken) =>
        {
            var report = await GenerateReportDataAsync(reportId, cancellationToken);
            return report;
        });

        // Handle the result and publish your own events
        var report = await taskResult;
        await _messagePublisher.PublishAsync(new ReportGeneratedEvent(reportId, report));
    }
}
```

## Migration from Previous Version

If you were using the previous version with MediatR event handling:

**Before:**
```csharp
_backgroundServiceFactory.EnqueueTask(
    async (token) => await DoWorkAsync(token),
    result => new TaskCompletedEvent(result)
);
```

**After:**
```csharp
var resultTask = _backgroundServiceFactory.EnqueueTask(
    async (token) => await DoWorkAsync(token)
);

// Handle completion and publish events yourself
var result = await resultTask;
await _yourMessageBroker.PublishAsync(new TaskCompletedEvent(result));
```

* * *