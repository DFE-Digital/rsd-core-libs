using MediatR;

namespace DfE.CoreLibs.AsyncProcessing.Interfaces
{
    public interface IBackgroundServiceEventHandler<in TEvent> : INotificationHandler<TEvent> where TEvent : IBackgroundServiceEvent
    {
    }
}
