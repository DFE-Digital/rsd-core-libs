namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
            where T : class;
    }
}
