using DfE.CoreLibs.AsyncProcessing.Interfaces;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using DfE.CoreLibs.AsyncProcessing.Configurations;

namespace DfE.CoreLibs.AsyncProcessing.Services
{
    public class BackgroundServiceFactory(IMediator mediator, IOptions<BackgroundServiceOptions> options) : BackgroundService, IBackgroundServiceFactory
    {
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        private readonly ConcurrentDictionary<Type, ConcurrentQueue<Func<CancellationToken, Task>>> _taskQueues = new();

        private readonly ConcurrentDictionary<Type, SemaphoreSlim> _semaphores = new();

        private CancellationToken _serviceStoppingToken = CancellationToken.None;

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
                if (options.Value.UseGlobalStoppingToken && callerCancellationToken.HasValue)
                {
                    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_serviceStoppingToken, callerCancellationToken.Value);
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
            });

            _ = StartProcessingQueue(taskType);
            return tcs.Task;
        }

        private async Task StartProcessingQueue(Type taskType)
        {
            Console.WriteLine("📌 StartProcessingQueue triggered");

            var queue = _taskQueues[taskType];
            var semaphore = _semaphores[taskType];

            // Ensure ExecuteAsync has set _serviceStoppingToken before processing
            while (options.Value.UseGlobalStoppingToken && _serviceStoppingToken == CancellationToken.None)
            {
                Console.WriteLine("⏳ Waiting for ExecuteAsync to initialize _serviceStoppingToken...");
                await Task.Delay(100);
            }

            Console.WriteLine($"🔍 _serviceStoppingToken IsCancellationRequested: {_serviceStoppingToken.IsCancellationRequested}");

            await semaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                while (queue.TryDequeue(out var taskToProcess))
                {
                    var token = options.Value.UseGlobalStoppingToken ? _serviceStoppingToken : CancellationToken.None;

                    Console.WriteLine($"⚡ Processing task with token IsCancellationRequested: {token.IsCancellationRequested}");
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
            Debug.WriteLine("🔵 ExecuteAsync started");

            if (options.Value.UseGlobalStoppingToken)
            {
                _serviceStoppingToken = stoppingToken;
                Debug.WriteLine("✅ _serviceStoppingToken assigned");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                Debug.WriteLine("⏳ ExecuteAsync loop running...");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
            }

            Debug.WriteLine("🛑 ExecuteAsync detected cancellation.");
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Debug.WriteLine("Background service is starting...");

            var executeTask = base.StartAsync(cancellationToken);

            await Task.Delay(500);

            Debug.WriteLine("Background service started, ExecuteAsync is now running.");

            await executeTask;
        }

    }
}
