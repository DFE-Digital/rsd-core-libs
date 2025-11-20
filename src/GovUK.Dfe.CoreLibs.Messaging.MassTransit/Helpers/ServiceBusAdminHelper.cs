using Azure.Messaging.ServiceBus.Administration;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Constants;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Helpers
{
    public static class ServiceBusAdminHelper
    {
        public static async Task EnsureEntitiesExistAsync(string connectionString, ILogger logger)
        {
            var adminClient = new ServiceBusAdministrationClient(connectionString);

            // Topics
            await EnsureTopicExists(adminClient, MessageEndpoints.TransferApplicationReceivedTopic);

            // Queues
            await EnsureQueueExists(adminClient, MessageEndpoints.ReportGenerationQueue);

            logger.LogInformation("Azure Service Bus: Topics and queues created dynamically via CoreLibs MassTransit library.");

        }

        private static async Task EnsureTopicExists(ServiceBusAdministrationClient client, string topicName)
        {
            if (!await client.TopicExistsAsync(topicName))
            {
                await client.CreateTopicAsync(new CreateTopicOptions(topicName)
                {
                    EnablePartitioning = true
                });
            }
        }

        private static async Task EnsureQueueExists(ServiceBusAdministrationClient client, string queueName)
        {
            if (!await client.QueueExistsAsync(queueName))
            {
                await client.CreateQueueAsync(new CreateQueueOptions(queueName)
                {
                    MaxDeliveryCount = 10,
                    LockDuration = TimeSpan.FromMinutes(5),
                    EnablePartitioning = true
                });
            }
        }
    }
}
