using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Interfaces;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Models;
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

        public Task PublishAsync<T>(T @event, AzureServiceBusMessageProperties messageProperties, CancellationToken cancellationToken = default)
            where T : class
        {
            return publishEndpoint.Publish(@event, context =>
            {
                ApplyMessageProperties(context, messageProperties);
            }, cancellationToken);
        }

        internal static void ApplyMessageProperties(PublishContext context, AzureServiceBusMessageProperties properties)
        {
            // Set system properties
            if (!string.IsNullOrWhiteSpace(properties.ContentType))
                context.ContentType = new System.Net.Mime.ContentType(properties.ContentType);

            if (!string.IsNullOrWhiteSpace(properties.CorrelationId))
                context.CorrelationId = Guid.Parse(properties.CorrelationId);

            if (!string.IsNullOrWhiteSpace(properties.MessageId))
                context.MessageId = Guid.Parse(properties.MessageId);

            if (properties.TimeToLive.HasValue)
                context.TimeToLive = properties.TimeToLive.Value;

            // Set Azure Service Bus specific headers
            if (!string.IsNullOrWhiteSpace(properties.PartitionKey))
                context.Headers.Set("PartitionKey", properties.PartitionKey);

            if (!string.IsNullOrWhiteSpace(properties.SessionId))
                context.Headers.Set("SessionId", properties.SessionId);

            if (!string.IsNullOrWhiteSpace(properties.ReplyTo))
                context.Headers.Set("ReplyTo", properties.ReplyTo);

            if (!string.IsNullOrWhiteSpace(properties.ReplyToSessionId))
                context.Headers.Set("ReplyToSessionId", properties.ReplyToSessionId);

            if (properties.ScheduledEnqueueTime.HasValue)
                context.Headers.Set("ScheduledEnqueueTimeUtc", properties.ScheduledEnqueueTime.Value);

            if (!string.IsNullOrWhiteSpace(properties.Subject))
                context.Headers.Set("Label", properties.Subject);

            if (!string.IsNullOrWhiteSpace(properties.To))
                context.Headers.Set("To", properties.To);

            // Set custom application properties
            foreach (var customProperty in properties.CustomProperties)
            {
                context.Headers.Set(customProperty.Key, customProperty.Value);
            }
        }
    }
}