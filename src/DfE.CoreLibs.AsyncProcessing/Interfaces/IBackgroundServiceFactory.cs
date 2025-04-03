namespace DfE.CoreLibs.AsyncProcessing.Interfaces
{
    public interface IBackgroundServiceFactory
    {
        /// <summary>
        /// Enqueues a background task with an optional event to be published when the task completes.
        /// The task will respect the service's stopping token if needed.
        /// </summary>
        /// <typeparam name="TResult">Return type of the task.</typeparam>
        /// <typeparam name="TEvent">Event type to publish upon task completion.</typeparam>
        /// <param name="taskFunc">
        /// A function representing the async work. Receives a CancellationToken if you want your task cancellable.
        /// </param>
        /// <param name="eventFactory">
        /// A factory function creating an event to publish when the task completes.
        /// </param>
        /// <param name="callerCancellationToken">
        /// The callers cancellation token.
        /// </param>
        Task<TResult> EnqueueTask<TResult, TEvent>(
            Func<CancellationToken, Task<TResult>> taskFunc,
            Func<TResult, TEvent>? eventFactory = null,
            CancellationToken? callerCancellationToken = null)
            where TEvent : IBackgroundServiceEvent;
    }
}
