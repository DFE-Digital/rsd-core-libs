using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Models;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Interfaces
{
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes an event to the message bus
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="event">The event to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
            where T : class;

        /// <summary>
        /// Publishes an event to the message bus with Azure Service Bus specific properties
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="event">The event to publish</param>
        /// <param name="messageProperties">Azure Service Bus message properties</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task PublishAsync<T>(T @event, AzureServiceBusMessageProperties messageProperties, CancellationToken cancellationToken = default)
            where T : class;
    }
}
