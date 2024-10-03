using MediatR;

namespace DfE.CoreLibs.BackgroundService.Interfaces
{
    public interface IBackgroundServiceEventHandler<in TEvent> : INotificationHandler<TEvent> where TEvent : IBackgroundServiceEvent
    {
    }
}
