using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Interfaces;
using MassTransit;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Publishers
{
    public class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
    {
        public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
            where T : class
        {
            return publishEndpoint.Publish(@event, cancellationToken);
        }
    }
}