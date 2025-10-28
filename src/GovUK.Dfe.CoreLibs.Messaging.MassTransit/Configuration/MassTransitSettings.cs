using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Enums;
using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Configuration
{
    public class MassTransitSettings
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TransportType Transport { get; set; } = TransportType.AzureServiceBus;
        public string AppPrefix { get; set; } = string.Empty;
        public AzureServiceBusSettings AzureServiceBus { get; set; } = new();
    }

    public class AzureServiceBusSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        
        /// <summary>
        /// If true, the client uses AMQP over WebSockets (port 443). If false, AMQP over TCP (port 5671).
        /// Default true for better compatibility behind corporate networks/proxies.
        /// </summary>
        public bool UseWebSockets { get; set; } = true;
        
        /// <summary>
        /// Determines whether to automatically create Azure Service Bus entities (topics/queues) at startup and during publishing.
        /// Default is false - entities should be managed externally (e.g., via Terraform, ARM templates, Azure Portal).
        /// Set to true only in development environments if you want automatic entity creation.
        /// </summary>
        public bool AutoCreateEntities { get; set; } = false;
        
        /// <summary>
        /// Determines whether to automatically configure receive endpoints using cfg.ConfigureEndpoints(context).
        /// Default is true - set to false if you want full manual control over endpoint configuration using SubscriptionEndpoint, ReceiveEndpoint, etc.
        /// When false, you must manually configure all endpoints in the configureAzureServiceBus callback.
        /// </summary>
        public bool AutoConfigureEndpoints { get; set; } = true;
    }
}
