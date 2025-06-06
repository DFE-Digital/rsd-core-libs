using DfE.CoreLibs.AsyncProcessing.Interfaces;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using DfE.CoreLibs.AsyncProcessing.Configurations;

namespace DfE.CoreLibs.AsyncProcessing.Services
{
    public class BackgroundServiceFactory(
        IMediator mediator,
        IOptions<BackgroundServiceOptions> options,
        ILogger<BackgroundServiceFactory> logger) : BackgroundService, IBackgroundServiceFactory
    {
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        private readonly ILogger<BackgroundServiceFactory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private readonly ConcurrentDictionary<Type, ConcurrentQueue<Func<CancellationToken, Task>>> _taskQueues = new();

        private readonly ConcurrentDictionary<Type, SemaphoreSlim> _semaphores = new();

        private CancellationToken _serviceStoppingToken = CancellationToken.None;

        private readonly TaskCompletionSource _tokenInitialisedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <inheritdoc />
        public Task<TResult> EnqueueTask<TResult, TEvent>(
            Func<CancellationToken, Task<TResult>> taskFunc,
            Func<TResult, TEvent>? eventFactory = null,
            CancellationToken? callerCancellationToken = null)
            where TEvent : IBackgroundServiceEvent
        {
            var tcs = new TaskCompletionSource<TResult>();

            var taskType = taskFunc.GetType();

            var queue = _taskQueues.GetOrAdd(taskType, _ => new ConcurrentQueue<Func<CancellationToken, Task>>());

            _semaphores.GetOrAdd(taskType, _ => new SemaphoreSlim(1, 1));

            queue.Enqueue(async (cancellationToken) =>
            {
                CancellationToken token;
                CancellationTokenSource? linkedCts = null;

                if (options.Value.UseGlobalStoppingToken && callerCancellationToken.HasValue)
                {
                    linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_serviceStoppingToken, callerCancellationToken.Value);
                    token = linkedCts.Token;
                }
                else if (options.Value.UseGlobalStoppingToken)
                {
                    token = _serviceStoppingToken;
                }
                else
                {
                    token = callerCancellationToken ?? CancellationToken.None;
                }

                try
                {
                    var result = await taskFunc(token).ConfigureAwait(false);

                    if (eventFactory != null)
                    {
                        var taskCompletedEvent = eventFactory.Invoke(result);
                        await _mediator.Publish(taskCompletedEvent, token).ConfigureAwait(false);
                    }
                    tcs.SetResult(result);
                }
                catch (OperationCanceledException)
                {
                    tcs.SetException(new OperationCanceledException());
                    throw;
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    throw;
                }
                finally
                {
                    linkedCts?.Dispose();
                }
            });

            var processingTask = StartProcessingQueue(taskType);
            _ = processingTask.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    _logger.LogError(t.Exception, "Error occurred while processing background queue for type {TaskType}", taskType);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
            return tcs.Task;
        }

        private async Task StartProcessingQueue(Type taskType)
        {
            _logger.LogDebug("StartProcessingQueue triggered for {TaskType}", taskType);

            var queue = _taskQueues[taskType];
            var semaphore = _semaphores[taskType];

            if (options.Value.UseGlobalStoppingToken)
            {
                await _tokenInitialisedTcs.Task.ConfigureAwait(false);
            }

            _logger.LogDebug("_serviceStoppingToken IsCancellationRequested: {IsCancellationRequested}", _serviceStoppingToken.IsCancellationRequested);

            await semaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                while (queue.TryDequeue(out var taskToProcess))
                {
                    var token = options.Value.UseGlobalStoppingToken ? _serviceStoppingToken : CancellationToken.None;

                    _logger.LogDebug("Processing task with token IsCancellationRequested: {IsCancellationRequested}", token.IsCancellationRequested);
                    await taskToProcess(token).ConfigureAwait(false);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("ExecuteAsync started");

            if (options.Value.UseGlobalStoppingToken)
            {
                _serviceStoppingToken = stoppingToken;
                _tokenInitialisedTcs.TrySetResult();
                _logger.LogDebug("_serviceStoppingToken assigned");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("ExecuteAsync loop running...");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
            }

            _logger.LogDebug("ExecuteAsync detected cancellation.");
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Background service is starting...");

            var executeTask = base.StartAsync(cancellationToken);

            await Task.Delay(500);

            _logger.LogDebug("Background service started, ExecuteAsync is now running.");

            await executeTask;
        }

    }
}
