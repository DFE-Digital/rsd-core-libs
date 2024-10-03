using System.Collections.Concurrent;
using DfE.CoreLibs.AsyncProcessing.Interfaces;
using MediatR;

namespace DfE.CoreLibs.AsyncProcessing.Services
{
    public class BackgroundServiceFactory(IMediator mediator) : Microsoft.Extensions.Hosting.BackgroundService, IBackgroundServiceFactory
    {
        private readonly ConcurrentDictionary<Type, ConcurrentQueue<Func<Task>>> _taskQueues = new();
        private readonly ConcurrentDictionary<Type, SemaphoreSlim> _semaphores = new();

        public void EnqueueTask<TResult, TEvent>(Func<Task<TResult>> taskFunc, Func<TResult, TEvent>? eventFactory = null)
            where TEvent : IBackgroundServiceEvent
        {
            var taskType = taskFunc.GetType();
            var queue = _taskQueues.GetOrAdd(taskType, new ConcurrentQueue<Func<Task>>());
            _semaphores.GetOrAdd(taskType, new SemaphoreSlim(1, 1));

            queue.Enqueue(async () =>
            {
                var result = await taskFunc();

                if (eventFactory != null)
                {
                    var taskCompletedEvent = eventFactory.Invoke(result);
                    await mediator.Publish(taskCompletedEvent);
                }
            });

            _ = StartProcessingQueue(taskType);
        }

        private async Task StartProcessingQueue(Type taskType)
        {
            var queue = _taskQueues[taskType];
            var semaphore = _semaphores[taskType];

            await semaphore.WaitAsync();

            try
            {
                while (queue.TryDequeue(out var taskToProcess))
                {
                    await taskToProcess();
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
