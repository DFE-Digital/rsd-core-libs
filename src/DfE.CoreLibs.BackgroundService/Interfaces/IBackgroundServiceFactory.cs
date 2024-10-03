namespace DfE.CoreLibs.BackgroundService.Interfaces
{
    public interface IBackgroundServiceFactory
    {
        void EnqueueTask<TResult, TEvent>(Func<Task<TResult>> taskFunc, Func<TResult, TEvent>? eventFactory = null)
            where TEvent : IBackgroundServiceEvent;
    }
}
