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
    }
}
