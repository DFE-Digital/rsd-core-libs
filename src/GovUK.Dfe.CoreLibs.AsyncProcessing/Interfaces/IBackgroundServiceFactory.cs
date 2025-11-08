namespace GovUK.Dfe.CoreLibs.AsyncProcessing.Interfaces
{
    public interface IBackgroundServiceFactory
    {
        /// <summary>
        /// Enqueues a background task to be processed asynchronously.
        /// The task will respect the service's stopping token if configured.
        /// </summary>
        /// <typeparam name="TResult">Return type of the task.</typeparam>
        /// <param name="taskFunc">
        /// A function representing the async work. Receives a CancellationToken if you want your task cancellable.
        /// </param>
        /// <param name="callerCancellationToken">
        /// Optional cancellation token from the caller to cancel the specific task.
        /// </param>
        /// <returns>A Task that completes when the background task completes, returning the result.</returns>
        Task<TResult> EnqueueTask<TResult>(
            Func<CancellationToken, Task<TResult>> taskFunc,
            CancellationToken? callerCancellationToken = null);
    }
}
