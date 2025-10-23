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
        /// Determines whether to automatically create Azure Service Bus entities (topics/queues) at startup.
        /// Default is true for backwards compatibility.
        /// Set to false if entities are managed externally (e.g., via Terraform, ARM templates).
        /// </summary>
        public bool AutoCreateEntities { get; set; } = true;
    }
}
