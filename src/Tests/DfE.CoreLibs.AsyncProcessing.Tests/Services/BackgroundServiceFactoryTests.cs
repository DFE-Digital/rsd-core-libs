using DfE.CoreLibs.AsyncProcessing.Configurations;
using DfE.CoreLibs.AsyncProcessing.Interfaces;
using DfE.CoreLibs.AsyncProcessing.Services;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using MediatR;
using NSubstitute;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace DfE.CoreLibs.AsyncProcessing.Tests.Services
{
    public class BackgroundServiceFactoryTests : IDisposable
    {
        private readonly IMediator _mediator;
        private readonly ILogger<BackgroundServiceFactory> _logger;
        private readonly BackgroundServiceFactory _factoryNoGlobalToken;
        private readonly BackgroundServiceFactory _factoryWithGlobalToken;

        private CancellationTokenSource _cts;


        public BackgroundServiceFactoryTests()
        {
            _mediator = Substitute.For<IMediator>();
            _logger = Substitute.For<ILogger<BackgroundServiceFactory>>();

            _cts = new CancellationTokenSource();

            var optionsNoGlobalToken = Options.Create(new BackgroundServiceOptions
            {
                UseGlobalStoppingToken = false
            });

            var optionsGlobalToken = Options.Create(new BackgroundServiceOptions
            {
                UseGlobalStoppingToken = true
            });

            _factoryNoGlobalToken = new BackgroundServiceFactory(_mediator, optionsNoGlobalToken, _logger);
            _factoryWithGlobalToken = new BackgroundServiceFactory(_mediator, optionsGlobalToken, _logger);
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        [Theory]
        [CustomAutoData]
        public async Task EnqueueTask_ShouldProcessTaskInQueue_WithoutGlobalToken(
                   Func<Task<int>> taskFunc,
                   IBackgroundServiceEvent eventMock)
        {
            // Arrange
            Func<int, IBackgroundServiceEvent> eventFactory = _ => eventMock;

            var semaphore = new SemaphoreSlim(0, 1);
            bool taskExecuted = false;

            async Task<int> WrappedTaskFunc(CancellationToken cancellationToken)
            {
                taskExecuted = true;
                semaphore.Release();
                return await taskFunc();
            }

            // Act
            _factoryNoGlobalToken.EnqueueTask(WrappedTaskFunc, eventFactory);

            var cts = new CancellationTokenSource();
            var runTask = _factoryNoGlobalToken.StartAsync(cts.Token);

            await semaphore.WaitAsync(TimeSpan.FromSeconds(1));

            cts.Cancel();
            await runTask;

            // Assert
            Assert.True(taskExecuted, "Task in the queue should have been processed (no global token).");
        }

        [Theory]
        [CustomAutoData]
        public async Task EnqueueTask_ShouldProcessTaskInQueue_WithGlobalToken(
            Func<Task<int>> taskFunc,
            IBackgroundServiceEvent eventMock)
        {
            // Arrange
            Func<int, IBackgroundServiceEvent> eventFactory = _ => eventMock;

            var semaphore = new SemaphoreSlim(0, 1);
            bool taskExecuted = false;

            async Task<int> WrappedTaskFunc(CancellationToken cancellationToken)
            {
                taskExecuted = true;
                semaphore.Release();
                return await taskFunc();
            }

            // Act
            _factoryWithGlobalToken.EnqueueTask(WrappedTaskFunc, eventFactory);

            var cts = new CancellationTokenSource();
            var runTask = _factoryWithGlobalToken.StartAsync(cts.Token);

            // Ensure the task gets processed
            await semaphore.WaitAsync(TimeSpan.FromSeconds(1));

            cts.Cancel();
            await runTask;

            // Assert
            Assert.True(taskExecuted, "Task in the queue should have been processed (global token).");
        }

        [Theory]
        [CustomAutoData]
        public async Task EnqueueTask_ShouldPublishEvent_WhenEventFactoryIsProvided_WithoutGlobalToken(
            int taskResult,
            IBackgroundServiceEvent eventMock)
        {
            // Arrange
            Func<int, IBackgroundServiceEvent> eventFactory = _ => eventMock;

            // Act
            _factoryNoGlobalToken.EnqueueTask(
                (cancellationToken) => Task.FromResult(taskResult),
                eventFactory);

            var cts = new CancellationTokenSource();
            var runTask = _factoryNoGlobalToken.StartAsync(cts.Token);

            await Task.Delay(100);
            cts.Cancel();
            await runTask;

            // Assert
            await _mediator.Received(1).Publish(eventMock, Arg.Any<CancellationToken>());
        }

        [Theory]
        [CustomAutoData]
        public async Task EnqueueTask_ShouldPublishEvent_WhenEventFactoryIsProvided_WithGlobalToken(
            int taskResult,
            IBackgroundServiceEvent eventMock)
        {
            // Arrange
            Func<int, IBackgroundServiceEvent> eventFactory = _ => eventMock;

            // Act
            _factoryWithGlobalToken.EnqueueTask(
                (cancellationToken) => Task.FromResult(taskResult),
                eventFactory);

            var cts = new CancellationTokenSource();
            var runTask = _factoryWithGlobalToken.StartAsync(cts.Token);

            await Task.Delay(100);
            cts.Cancel();
            await runTask;

            // Assert
            await _mediator.Received(1).Publish(eventMock, Arg.Any<CancellationToken>());
        }

        [Theory]
        [CustomAutoData]
        public async Task StartProcessingQueue_ShouldProcessTasksSequentially_WithoutGlobalToken(
            Func<Task<int>> taskFunc,
            IBackgroundServiceEvent eventMock)
        {
            // Arrange
            Func<int, IBackgroundServiceEvent> eventFactory = _ => eventMock;

            var taskCount = 0;

            Func<CancellationToken, Task<int>> wrappedTaskFunc = async (cancellationToken) =>
            {
                Interlocked.Increment(ref taskCount);
                return await taskFunc();
            };

            _factoryNoGlobalToken.EnqueueTask(wrappedTaskFunc, eventFactory);
            _factoryNoGlobalToken.EnqueueTask(wrappedTaskFunc, eventFactory);

            var cts = new CancellationTokenSource();
            var runTask = _factoryNoGlobalToken.StartAsync(cts.Token);

            // Act
            await Task.Delay(200);
            cts.Cancel();
            await runTask;

            // Assert
            Assert.Equal(2, taskCount);
        }

        [Theory]
        [CustomAutoData]
        public async Task StartProcessingQueue_ShouldProcessTasksSequentially_WithGlobalToken(
            Func<Task<int>> taskFunc,
            IBackgroundServiceEvent eventMock)
        {
            // Arrange
            Func<int, IBackgroundServiceEvent> eventFactory = _ => eventMock;

            var taskCount = 0;

            Func<CancellationToken, Task<int>> wrappedTaskFunc = async (cancellationToken) =>
            {
                Interlocked.Increment(ref taskCount);
                return await taskFunc();
            };

            _factoryWithGlobalToken.EnqueueTask(wrappedTaskFunc, eventFactory);
            _factoryWithGlobalToken.EnqueueTask(wrappedTaskFunc, eventFactory);

            var cts = new CancellationTokenSource();
            var runTask = _factoryWithGlobalToken.StartAsync(cts.Token);

            // Act
            await Task.Delay(200);
            cts.Cancel();
            await runTask;

            // Assert
            Assert.Equal(2, taskCount);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldStopProcessing_WhenCancellationRequested_NoGlobalToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var runTask = _factoryNoGlobalToken.StartAsync(cts.Token);

            // Act
            cts.Cancel();
            await runTask;

            // Assert
            Assert.True(runTask.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldStopProcessing_WhenCancellationRequested_WithGlobalToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var runTask = _factoryWithGlobalToken.StartAsync(cts.Token);

            // Act
            cts.Cancel();
            await runTask;

            // Assert
            Assert.True(runTask.IsCompletedSuccessfully);
        }

        // New test 1: If a task is long-running and UseGlobalStoppingToken = true,
        // it should see the token canceled. We can check whether the task
        // respects the token by having it throw OperationCanceledException, for instance.
        [Fact]
        public async Task LongRunningTask_ShouldBeCanceled_WhenUseGlobalStoppingToken_True()
        {
            var runTask = _factoryWithGlobalToken.StartAsync(_cts.Token);
            await Task.Delay(500);

            var enqueuedTask = _factoryWithGlobalToken.EnqueueTask<bool, IBackgroundServiceEvent>(async (token) =>
            {
                for (int i = 0; i < 10; i++)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(500, token);
                    token.ThrowIfCancellationRequested();
                }
                return false;
            }, null);

            await Task.Delay(600); 
            _cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await enqueuedTask);
            await runTask; 
        }

        // New test 2: If UseGlobalStoppingToken = false, the long-running task
        // won't be canceled - it runs to completion (or times out).
        [Fact]
        public async Task LongRunningTask_ShouldNotBeCanceled_WhenUseGlobalStoppingToken_False()
        {
            var cts = new CancellationTokenSource();

            var wasCanceled = false;

            _factoryNoGlobalToken.EnqueueTask<bool, IBackgroundServiceEvent>(async (token) =>
            {
                try
                {
                    for (int i = 0; i < 3; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        await Task.Delay(300);
                    }
                    return false;
                }
                catch (OperationCanceledException)
                {
                    wasCanceled = true;
                    throw;
                }
            }, null);

            var runTask = _factoryNoGlobalToken.StartAsync(cts.Token);

            await Task.Delay(200);
            cts.Cancel();

            await runTask;

            // Since we do not use the global token in tasks, wasCanceled should remain false
            // The task will keep running to completion but eventually the service loop ends.
            Assert.False(wasCanceled, "Task should NOT see the cancellation token if UseGlobalStoppingToken = false.");
        }

        [Fact]
        public async Task Task_ShouldBeCanceled_IfCallerTokenIsUsedAndCanceled()
        {
            var runTask = _factoryNoGlobalToken.StartAsync(_cts.Token);
            await Task.Delay(200);

            var callerCts = new CancellationTokenSource();

            var enqueuedTask = _factoryNoGlobalToken.EnqueueTask<bool, IBackgroundServiceEvent>(async (token) =>
            {
                for (int i = 0; i < 5; i++)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(200, token);
                }
                return false;
            }, null, callerCts.Token);

            await Task.Delay(400);
            callerCts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await enqueuedTask);
            await runTask;
        }

        [Fact]
        public async Task Task_ShouldBeCanceled_IfEitherGlobalOrCallerTokenIsCanceled()
        {
            var runTask = _factoryWithGlobalToken.StartAsync(_cts.Token);
            await Task.Delay(200);

            var callerCts = new CancellationTokenSource();

            var enqueuedTask = _factoryWithGlobalToken.EnqueueTask<bool, IBackgroundServiceEvent>(async (token) =>
            {
                for (int i = 0; i < 5; i++)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(300, token);
                }
                return false;
            }, null, callerCts.Token);

            // Cancel either global or caller token
            await Task.Delay(400);
            callerCts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await enqueuedTask);
            await runTask;
        }

        [Fact]
        public async Task Task_ShouldCompleteSuccessfully_IfNoTokenIsCanceled()
        {
            var runTask = _factoryWithGlobalToken.StartAsync(_cts.Token);

            var enqueuedTask = _factoryWithGlobalToken.EnqueueTask<bool, IBackgroundServiceEvent>(async (token) =>
            {
                await Task.Delay(200, token);
                return true;
            }, null, CancellationToken.None);

            var result = await enqueuedTask;
            _cts.Cancel();
            await runTask;

            Assert.True(result);
        }

        [Fact]
        public async Task Task_ShouldNotProcessUntilServiceStarted_WhenUseGlobalStoppingToken()
        {
            var processed = false;

            _factoryWithGlobalToken.EnqueueTask<bool, IBackgroundServiceEvent>(token =>
            {
                processed = true;
                return Task.FromResult(true);
            }, null);

            await Task.Delay(200);

            Assert.False(processed, "Task should not execute before StartAsync assigns the global token.");

            var runTask = _factoryWithGlobalToken.StartAsync(_cts.Token);

            await Task.Delay(200);

            _cts.Cancel();
            await runTask;

            Assert.True(processed, "Task should execute after the service starts and global token is assigned.");
        }

        [Fact]
        public async Task EnqueueTask_ShouldLogError_WhenTaskThrows()
        {
            // Arrange
            _factoryNoGlobalToken.EnqueueTask<int, IBackgroundServiceEvent>(token => throw new InvalidOperationException("boom"));

            var cts = new CancellationTokenSource();
            var runTask = _factoryNoGlobalToken.StartAsync(cts.Token);

            await Task.Delay(200);
            cts.Cancel();
            await runTask;

            // Assert
            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains("Error occurred while processing background queue")),
                Arg.Is<AggregateException>(ex =>
                    ex.InnerException.Message == "boom"),
                Arg.Any<Func<object, Exception?, string>>());
        }


    }
}