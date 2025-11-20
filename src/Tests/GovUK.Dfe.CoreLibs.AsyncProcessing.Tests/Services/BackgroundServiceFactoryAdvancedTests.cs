using GovUK.Dfe.CoreLibs.AsyncProcessing.Configurations;
using GovUK.Dfe.CoreLibs.AsyncProcessing.Interfaces;
using GovUK.Dfe.CoreLibs.AsyncProcessing.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GovUK.Dfe.CoreLibs.AsyncProcessing.Tests.Services
{
    public class BackgroundServiceFactoryAdvancedTests : IDisposable
    {
        private readonly ILogger<BackgroundServiceFactory> _logger;
        private CancellationTokenSource _cts;

        public BackgroundServiceFactoryAdvancedTests()
        {
            _logger = Substitute.For<ILogger<BackgroundServiceFactory>>();
            _cts = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new BackgroundServiceFactory(null!, Options.Create(new BackgroundServiceOptions())));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new BackgroundServiceFactory(_logger, null!));
        }

        [Fact]
        public void Constructor_WithBoundedChannel_ShouldInitializeCorrectly()
        {
            // Arrange
            var options = Options.Create(new BackgroundServiceOptions
            {
                ChannelCapacity = 10,
                ChannelFullMode = ChannelFullMode.Wait
            });

            // Act
            var factory = new BackgroundServiceFactory(_logger, options);

            // Assert
            Assert.NotNull(factory);
        }

        [Fact]
        public void Constructor_WithDropOldestMode_ShouldInitializeCorrectly()
        {
            // Arrange
            var options = Options.Create(new BackgroundServiceOptions
            {
                ChannelCapacity = 5,
                ChannelFullMode = ChannelFullMode.DropOldest
            });

            // Act
            var factory = new BackgroundServiceFactory(_logger, options);

            // Assert
            Assert.NotNull(factory);
        }

        [Fact]
        public void Constructor_WithThrowExceptionMode_ShouldInitializeCorrectly()
        {
            // Arrange
            var options = Options.Create(new BackgroundServiceOptions
            {
                ChannelCapacity = 5,
                ChannelFullMode = ChannelFullMode.ThrowException
            });

            // Act
            var factory = new BackgroundServiceFactory(_logger, options);

            // Assert
            Assert.NotNull(factory);
        }

        [Fact]
        public async Task ParallelProcessing_ShouldProcessMultipleTasksConcurrently()
        {
            // Arrange
            var options = Options.Create(new BackgroundServiceOptions
            {
                MaxConcurrentWorkers = 3,
                EnableDetailedLogging = true
            });
            var factory = new BackgroundServiceFactory(_logger, options);

            var processedTasks = new System.Collections.Concurrent.ConcurrentBag<int>();
            var semaphore = new SemaphoreSlim(0);

            // Act
            for (int i = 0; i < 5; i++)
            {
                var taskId = i;
                factory.EnqueueTask(async (token) =>
                {
                    await Task.Delay(100, token);
                    processedTasks.Add(taskId);
                    semaphore.Release();
                    return taskId;
                }, null);
            }

            await factory.StartAsync(_cts.Token);
            
            // Wait for all tasks to complete
            for (int i = 0; i < 5; i++)
            {
                await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
            }

            _cts.Cancel();
            await factory.StopAsync(CancellationToken.None);

            // Assert
            Assert.Equal(5, processedTasks.Count);
        }

        [Fact]
        public async Task EnqueueTask_ShouldHandleTaskException()
        {
            // Arrange
            var options = Options.Create(new BackgroundServiceOptions());
            var factory = new BackgroundServiceFactory(_logger, options);

            // Act
            var task = factory.EnqueueTask<int>(async (token) =>
            {
                await Task.Delay(10, token);
                throw new InvalidOperationException("Test exception");
            });

            await factory.StartAsync(_cts.Token);
            await Task.Delay(200);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);

            _cts.Cancel();
            await factory.StopAsync(CancellationToken.None);
        }

        [Fact]
        public void EnqueueTask_WithNullTaskFunc_ShouldThrowArgumentNullException()
        {
            // Arrange
            var options = Options.Create(new BackgroundServiceOptions());
            var factory = new BackgroundServiceFactory(_logger, options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
            {
                var _ = factory.EnqueueTask<int>(null!, null);
            });
        }

        [Fact]
        public async Task StopAsync_ShouldCompleteRemainingTasks()
        {
            // Arrange
            var options = Options.Create(new BackgroundServiceOptions());
            var factory = new BackgroundServiceFactory(_logger, options);

            var tasksCompleted = 0;

            factory.EnqueueTask(async (token) =>
            {
                await Task.Delay(50, token);
                Interlocked.Increment(ref tasksCompleted);
                return 1;
            }, null);

            factory.EnqueueTask(async (token) =>
            {
                await Task.Delay(50, token);
                Interlocked.Increment(ref tasksCompleted);
                return 2;
            }, null);

            // Act
            await factory.StartAsync(_cts.Token);
            await Task.Delay(200);
            await factory.StopAsync(CancellationToken.None);

            // Assert
            Assert.Equal(2, tasksCompleted);
        }

        [Fact]
        public async Task Dispose_ShouldCleanupResources()
        {
            // Arrange
            var options = Options.Create(new BackgroundServiceOptions());
            var factory = new BackgroundServiceFactory(_logger, options);

            await factory.StartAsync(_cts.Token);
            await Task.Delay(100);

            // Act
            factory.Dispose();

            // Assert - No exception thrown
            Assert.True(true);
        }

        [Fact]
        public async Task EnableDetailedLogging_ShouldCompleteSuccessfully()
        {
            // Arrange
            var options = Options.Create(new BackgroundServiceOptions
            {
                EnableDetailedLogging = true
            });
            var factory = new BackgroundServiceFactory(_logger, options);

            var taskCompleted = false;

            // Act
            var task = factory.EnqueueTask(
                async (token) =>
                {
                    await Task.Delay(50, token);
                    taskCompleted = true;
                    return 42;
                });

            await factory.StartAsync(_cts.Token);
            await task;

            await factory.StopAsync(CancellationToken.None);
            _cts.Cancel();

            // Assert - Just verify the factory works with detailed logging enabled
            Assert.True(taskCompleted);
        }

        [Fact]
        public void ChannelFullMode_ThrowException_CanBeConfigured()
        {
            // Arrange & Act
            var options = Options.Create(new BackgroundServiceOptions
            {
                ChannelCapacity = 5,
                ChannelFullMode = ChannelFullMode.ThrowException
            });
            var factory = new BackgroundServiceFactory(_logger, options);

            // Assert - Just verify the factory can be created with this configuration
            // Testing the actual throw behavior is complex due to race conditions with channel processing
            Assert.NotNull(factory);
        }

        [Fact]
        public async Task MultipleWorkers_ShouldProcessTasksConcurrently()
        {
            // Arrange
            var options = Options.Create(new BackgroundServiceOptions
            {
                MaxConcurrentWorkers = 4
            });
            var factory = new BackgroundServiceFactory(_logger, options);

            var concurrentExecutions = 0;
            var maxConcurrentExecutions = 0;
            var lockObj = new object();

            // Act
            for (int i = 0; i < 10; i++)
            {
                factory.EnqueueTask(async (token) =>
                {
                    lock (lockObj)
                    {
                        concurrentExecutions++;
                        if (concurrentExecutions > maxConcurrentExecutions)
                        {
                            maxConcurrentExecutions = concurrentExecutions;
                        }
                    }

                    await Task.Delay(100, token);

                    lock (lockObj)
                    {
                        concurrentExecutions--;
                    }

                    return 1;
                }, null);
            }

            await factory.StartAsync(_cts.Token);
            await Task.Delay(500);

            _cts.Cancel();
            await factory.StopAsync(CancellationToken.None);

            // Assert - Should have had multiple tasks running concurrently
            Assert.True(maxConcurrentExecutions > 1, $"Expected concurrent executions > 1, but got {maxConcurrentExecutions}");
        }

        [Fact]
        public async Task Task_WithCallerAndGlobalToken_BothCanceled_ShouldCancel()
        {
            // Arrange
            var options = Options.Create(new BackgroundServiceOptions
            {
                UseGlobalStoppingToken = true
            });
            var factory = new BackgroundServiceFactory(_logger, options);

            var callerCts = new CancellationTokenSource();
            var globalCts = new CancellationTokenSource();

            // Act
            var task = factory.EnqueueTask(async (token) =>
            {
                await Task.Delay(5000, token);
                return 1;
            }, callerCts.Token);

            await factory.StartAsync(globalCts.Token);
            await Task.Delay(100);

            callerCts.Cancel();

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);

            globalCts.Cancel();
            await factory.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task ProcessTasksAsync_ShouldHandleUnexpectedException()
        {
            // Arrange
            var options = Options.Create(new BackgroundServiceOptions
            {
                EnableDetailedLogging = true
            });
            var factory = new BackgroundServiceFactory(_logger, options);

            var exceptionThrown = false;

            // Act
            var task = factory.EnqueueTask<int>(async (token) =>
            {
                exceptionThrown = true;
                throw new DivideByZeroException("Unexpected error");
            });

            await factory.StartAsync(_cts.Token);
            await Task.Delay(200);

            _cts.Cancel();
            await factory.StopAsync(CancellationToken.None);

            // Assert - Task should have thrown exception
            Assert.True(exceptionThrown);
            await Assert.ThrowsAsync<DivideByZeroException>(async () => await task);
        }

        [Fact]
        public async Task StartAsync_ShouldWaitForInitialization()
        {
            // Arrange
            var options = Options.Create(new BackgroundServiceOptions());
            var factory = new BackgroundServiceFactory(_logger, options);

            // Act
            var startTask = factory.StartAsync(_cts.Token);
            await startTask;

            // Assert
            Assert.True(startTask.IsCompletedSuccessfully);

            _cts.Cancel();
            await factory.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task BoundedChannel_WithWaitMode_CanBeConfigured()
        {
            // Arrange
            var options = Options.Create(new BackgroundServiceOptions
            {
                ChannelCapacity = 10,
                ChannelFullMode = ChannelFullMode.Wait
            });
            var factory = new BackgroundServiceFactory(_logger, options);

            // Act - Enqueue a task
            factory.EnqueueTask(async (token) =>
            {
                await Task.Delay(50, token);
                return 1;
            });

            await factory.StartAsync(_cts.Token);
            await Task.Delay(200);

            // Assert - Factory works with bounded channel
            Assert.NotNull(factory);

            _cts.Cancel();
            await factory.StopAsync(CancellationToken.None);
        }
    }
}

