using System.Diagnostics;
using System.Threading.Channels;
using GovUK.Dfe.CoreLibs.AsyncProcessing.Configurations;
using GovUK.Dfe.CoreLibs.AsyncProcessing.Interfaces;
using GovUK.Dfe.CoreLibs.AsyncProcessing.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.AsyncProcessing.Services
{
    public class BackgroundServiceFactory : BackgroundService, IBackgroundServiceFactory
    {
        private readonly ILogger<BackgroundServiceFactory> _logger;
        private readonly BackgroundServiceOptions _options;
        private readonly Channel<TaskWorkItem> _taskChannel;
        private readonly Task[] _workerTasks;
        private CancellationToken _serviceStoppingToken;
        private readonly SemaphoreSlim _startupSemaphore = new(0, 1);
        
        // Metrics
        private long _totalEnqueued;
        private long _totalProcessed;
        private long _totalFailed;

        public BackgroundServiceFactory(
            ILogger<BackgroundServiceFactory> logger,
            IOptions<BackgroundServiceOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            // Configure channel based on options
            var channelOptions = _options.ChannelCapacity == int.MaxValue
                ? new UnboundedChannelOptions 
                { 
                    SingleReader = _options.MaxConcurrentWorkers == 1,
                    SingleWriter = false 
                }
                : (ChannelOptions)new BoundedChannelOptions(_options.ChannelCapacity)
                {
                    FullMode = _options.ChannelFullMode switch
                    {
                        ChannelFullMode.DropOldest => BoundedChannelFullMode.DropOldest,
                        ChannelFullMode.ThrowException => BoundedChannelFullMode.DropWrite,
                        _ => BoundedChannelFullMode.Wait
                    },
                    SingleReader = _options.MaxConcurrentWorkers == 1,
                    SingleWriter = false
                };

            _taskChannel = _options.ChannelCapacity == int.MaxValue
                ? Channel.CreateUnbounded<TaskWorkItem>((UnboundedChannelOptions)channelOptions)
                : Channel.CreateBounded<TaskWorkItem>((BoundedChannelOptions)channelOptions);

            // Initialize worker tasks array
            _workerTasks = new Task[_options.MaxConcurrentWorkers];

            _logger.LogInformation(
                "BackgroundServiceFactory initialized with {WorkerCount} workers, capacity: {Capacity}, " +
                "UseGlobalStoppingToken: {UseGlobalToken}",
                _options.MaxConcurrentWorkers,
                _options.ChannelCapacity == int.MaxValue ? "Unbounded" : _options.ChannelCapacity.ToString(),
                _options.UseGlobalStoppingToken);
        }

        /// <inheritdoc />
        public Task<TResult> EnqueueTask<TResult>(
            Func<CancellationToken, Task<TResult>> taskFunc,
            CancellationToken? callerCancellationToken = null)
        {
            ArgumentNullException.ThrowIfNull(taskFunc);

            var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var taskType = taskFunc.GetType();

            // Wrap the task with completion handling
            async Task ExecuteWrappedTask(CancellationToken cancellationToken)
            {
                CancellationTokenSource? linkedCts = null;
                try
                {
                    // Determine which cancellation token to use
                    var effectiveToken = DetermineEffectiveToken(callerCancellationToken, cancellationToken, out linkedCts);

                    if (_options.EnableDetailedLogging)
                    {
                        _logger.LogDebug("Executing task of type {TaskType}", taskType.Name);
                    }

                    // Execute the actual task
                    var result = await taskFunc(effectiveToken).ConfigureAwait(false);

                    // Complete the task successfully
                    tcs.TrySetResult(result);
                    Interlocked.Increment(ref _totalProcessed);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Task of type {TaskType} was canceled", taskType.Name);
                    tcs.TrySetCanceled(cancellationToken);
                    Interlocked.Increment(ref _totalFailed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Task of type {TaskType} failed with exception", taskType.Name);
                    tcs.TrySetException(ex);
                    Interlocked.Increment(ref _totalFailed);
                }
                finally
                {
                    linkedCts?.Dispose();
                }
            }

            // Create work item
            var workItem = new TaskWorkItem
            {
                ExecuteAsync = ExecuteWrappedTask,
                TaskType = taskType,
                CallerToken = callerCancellationToken
            };

            // Enqueue to channel
            if (!_taskChannel.Writer.TryWrite(workItem))
            {
                // Channel is full and configured to throw
                if (_options.ChannelFullMode == ChannelFullMode.ThrowException)
                {
                    var ex = new InvalidOperationException(
                        $"Task channel is full (capacity: {_options.ChannelCapacity}). Cannot enqueue task.");
                    tcs.SetException(ex);
                    _logger.LogWarning(ex, "Failed to enqueue task of type {TaskType}", taskType.Name);
                    return tcs.Task;
                }

                // For Wait mode, this will block until space is available
                _taskChannel.Writer.WriteAsync(workItem).AsTask().Wait();
            }

            Interlocked.Increment(ref _totalEnqueued);

            if (_options.EnableDetailedLogging)
            {
                _logger.LogDebug("Task of type {TaskType} enqueued. Queue stats: Enqueued={Enqueued}, " +
                    "Processed={Processed}, Failed={Failed}",
                    taskType.Name, _totalEnqueued, _totalProcessed, _totalFailed);
            }

            return tcs.Task;
        }

        private CancellationToken DetermineEffectiveToken(
            CancellationToken? callerToken,
            CancellationToken workerToken,
            out CancellationTokenSource? linkedCts)
        {
            linkedCts = null;

            if (_options.UseGlobalStoppingToken && callerToken.HasValue)
            {
                // Link both global and caller tokens
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(workerToken, callerToken.Value);
                return linkedCts.Token;
            }

            if (_options.UseGlobalStoppingToken)
            {
                // Use global token only
                return workerToken;
            }

            // Use caller token or none
            return callerToken ?? CancellationToken.None;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _serviceStoppingToken = stoppingToken;

            _logger.LogInformation("BackgroundServiceFactory starting {WorkerCount} worker(s)", 
                _options.MaxConcurrentWorkers);

            // Signal that service has started
            _startupSemaphore.Release();

            try
            {
                // Start all worker tasks
                for (int i = 0; i < _options.MaxConcurrentWorkers; i++)
                {
                    var workerId = i;
                    _workerTasks[i] = ProcessTasksAsync(workerId, stoppingToken);
                }

                // Wait for all workers to complete
                await Task.WhenAll(_workerTasks).ConfigureAwait(false);

                _logger.LogInformation("All workers completed. Stats: Enqueued={Enqueued}, " +
                    "Processed={Processed}, Failed={Failed}",
                    _totalEnqueued, _totalProcessed, _totalFailed);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("BackgroundServiceFactory is shutting down");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BackgroundServiceFactory encountered an error");
                throw;
            }
            finally
            {
                // Complete the channel to signal no more writes
                _taskChannel.Writer.Complete();
            }
        }

        private async Task ProcessTasksAsync(int workerId, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker {WorkerId} started", workerId);

            try
            {
                // Read from channel until it's completed or cancellation requested
                await foreach (var workItem in _taskChannel.Reader.ReadAllAsync(stoppingToken))
                {
                    var stopwatch = Stopwatch.StartNew();

                    try
                    {
                        var queueTime = (DateTime.UtcNow - workItem.EnqueuedAt).TotalMilliseconds;
                        
                        if (_options.EnableDetailedLogging)
                        {
                            _logger.LogDebug("Worker {WorkerId} processing task {TaskType}. Queue time: {QueueTime}ms",
                                workerId, workItem.TaskType.Name, queueTime);
                        }

                        await workItem.ExecuteAsync(stoppingToken).ConfigureAwait(false);

                        stopwatch.Stop();

                        if (_options.EnableDetailedLogging)
                        {
                            _logger.LogDebug("Worker {WorkerId} completed task {TaskType} in {ElapsedMs}ms",
                                workerId, workItem.TaskType.Name, stopwatch.ElapsedMilliseconds);
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogDebug("Worker {WorkerId} task canceled due to shutdown", workerId);
                        throw; // Re-throw to exit the loop
                    }
                    catch (Exception ex)
                    {
                        // Task exceptions are already handled in ExecuteWrappedTask
                        // This catches any unexpected exceptions
                        _logger.LogError(ex, "Worker {WorkerId} encountered unexpected error processing task {TaskType}",
                            workerId, workItem.TaskType.Name);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker {WorkerId} stopping due to cancellation", workerId);
            }
            finally
            {
                _logger.LogInformation("Worker {WorkerId} stopped", workerId);
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BackgroundServiceFactory is starting...");

            // Start the background service
            await base.StartAsync(cancellationToken).ConfigureAwait(false);

            // Wait for ExecuteAsync to initialize (with timeout)
            await _startupSemaphore.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("BackgroundServiceFactory started successfully");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BackgroundServiceFactory is stopping...");

            // Complete the channel writer to signal no more items
            _taskChannel.Writer.Complete();

            // Wait for workers to finish processing remaining items
            await base.StopAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("BackgroundServiceFactory stopped. Final stats: Enqueued={Enqueued}, " +
                "Processed={Processed}, Failed={Failed}",
                _totalEnqueued, _totalProcessed, _totalFailed);
        }

        public override void Dispose()
        {
            _startupSemaphore?.Dispose();
            base.Dispose();
        }
    }
}
