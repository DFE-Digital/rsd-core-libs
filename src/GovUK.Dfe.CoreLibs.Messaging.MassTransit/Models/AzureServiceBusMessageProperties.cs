namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Models
{
    /// <summary>
    /// Represents Azure Service Bus message properties including system properties and custom properties
    /// </summary>
    public class AzureServiceBusMessageProperties
    {
        /// <summary>
        /// Gets or sets the content type of the message (e.g., "application/json")
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Gets or sets the correlation identifier for request-reply patterns
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the message identifier
        /// </summary>
        public string? MessageId { get; set; }

        /// <summary>
        /// Gets or sets the partition key for partitioned entities
        /// </summary>
        public string? PartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the session identifier for session-aware entities
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Gets or sets the reply-to address
        /// </summary>
        public string? ReplyTo { get; set; }

        /// <summary>
        /// Gets or sets the reply-to session identifier
        /// </summary>
        public string? ReplyToSessionId { get; set; }

        /// <summary>
        /// Gets or sets the time to live for the message
        /// </summary>
        public TimeSpan? TimeToLive { get; set; }

        /// <summary>
        /// Gets or sets the scheduled enqueue time (for delayed messages)
        /// </summary>
        public DateTimeOffset? ScheduledEnqueueTime { get; set; }

        /// <summary>
        /// Gets or sets the subject/label for the message
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the message's "to" address
        /// </summary>
        public string? To { get; set; }

        /// <summary>
        /// Gets or sets custom application properties (key-value pairs)
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new();

        /// <summary>
        /// Adds a custom property to the message
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        /// <returns>The current instance for fluent configuration</returns>
        public AzureServiceBusMessageProperties AddCustomProperty(string key, object value)
        {
            CustomProperties[key] = value;
            return this;
        }

        /// <summary>
        /// Adds multiple custom properties to the message
        /// </summary>
        /// <param name="properties">Dictionary of properties to add</param>
        /// <returns>The current instance for fluent configuration</returns>
        public AzureServiceBusMessageProperties AddCustomProperties(Dictionary<string, object> properties)
        {
            foreach (var property in properties)
            {
                CustomProperties[property.Key] = property.Value;
            }
            return this;
        }
    }
}

