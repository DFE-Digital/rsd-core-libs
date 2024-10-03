# DfE.CoreLibs.BackgroundService

This library provides a robust framework for implementing long-running background tasks in .NET applications. It simplifies the development of background services by offering reusable components that streamline task scheduling, execution, and error handling. Ideal for any project requiring background processing, it ensures reliability and scalability across different environments.

## Installation

To install the DfE.CoreLibs.BackgroundService Library, use the following command in your .NET project:

```sh
dotnet add package DfE.CoreLibs.BackgroundService
```

## Usage

**Usage in a Command Handler**

1.  **Service Registration:** You use a background service factory to enqueue tasks. Register the factory in your `Program.cs`:

    ```csharp
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBackgroundService();
        }
    ```
    

2.  **Implementation in the Handler:** You enqueue tasks using `IBackgroundServiceFactory` directly inside a command handler, optionally you can pass in an event to be raised when the task is completed, as shown in your code:

    ```csharp
    public class CreateReportCommandHandler : IRequestHandler<CreateReportCommand, bool>
    {
        private readonly IBackgroundServiceFactory _backgroundServiceFactory;
    
        public CreateReportCommandHandler(IBackgroundServiceFactory backgroundServiceFactory)
        {
            _backgroundServiceFactory = backgroundServiceFactory;
        }
    
        public Task<bool> Handle(CreateReportCommand request, CancellationToken cancellationToken)
        {
            var taskName = "Create_Report_Task1";
    
            _backgroundServiceFactory.EnqueueTask(
                async () => await (new CreateReportExampleTask()).RunAsync(taskName),
                result => new CreateReportExampleTaskCompletedEvent(taskName, result)
            );
    
            return Task.FromResult(true);
        }
    }
    ```

3.  **Events:** The background service triggers events when a task is completed. For example:

    ```csharp
    public class CreateReportExampleTaskCompletedEvent : IBackgroundServiceEvent
    {
        public string TaskName { get; }
        public string Message { get; }
    
        public CreateReportExampleTaskCompletedEvent(string taskName, string message)
        {
            TaskName = taskName;
            Message = message;
        }
    }
    ```

4.  **Event Handlers:** These events are processed by event handlers. Here's an example of how you handle task completion events:

    ```csharp
    public class SimpleTaskCompletedEventHandler : IBackgroundServiceEventHandler<CreateReportExampleTaskCompletedEvent>
    {
        public Task Handle(CreateReportExampleTaskCompletedEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Event received for Task: {notification.TaskName}, Message: {notification.Message}");
            return Task.CompletedTask;
        }
    }
    ```

This setup allows you to enqueue tasks in the background, fire events when tasks complete, and handle those events using a custom event handler architecture.

* * *