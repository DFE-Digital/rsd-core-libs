using MediatR;

namespace GovUK.Dfe.CoreLibs.AsyncProcessing.Interfaces
{
    public interface IBackgroundServiceEventHandler<in TEvent> : INotificationHandler<TEvent> where TEvent : IBackgroundServiceEvent
    {
    }
}
