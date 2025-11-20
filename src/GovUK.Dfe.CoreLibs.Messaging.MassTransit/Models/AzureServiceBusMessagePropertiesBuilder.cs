namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Models
{
    /// <summary>
    /// Fluent builder for Azure Service Bus message properties
    /// </summary>
    public class AzureServiceBusMessagePropertiesBuilder
    {
        private readonly AzureServiceBusMessageProperties _properties = new();

        /// <summary>
        /// Sets the content type of the message
        /// </summary>
        /// <param name="contentType">Content type (e.g., "application/json")</param>
        /// <returns>The builder instance for fluent configuration</returns>
        public AzureServiceBusMessagePropertiesBuilder WithContentType(string contentType)
        {
            _properties.ContentType = contentType;
            return this;
        }

        /// <summary>
        /// Sets the correlation identifier
        /// </summary>
        /// <param name="correlationId">Correlation ID</param>
        /// <returns>The builder instance for fluent configuration</returns>
        public AzureServiceBusMessagePropertiesBuilder WithCorrelationId(string correlationId)
        {
            _properties.CorrelationId = correlationId;
            return this;
        }

        /// <summary>
        /// Sets the message identifier
        /// </summary>
        /// <param name="messageId">Message ID</param>
        /// <returns>The builder instance for fluent configuration</returns>
        public AzureServiceBusMessagePropertiesBuilder WithMessageId(string messageId)
        {
            _properties.MessageId = messageId;
            return this;
        }

        /// <summary>
        /// Sets the partition key
        /// </summary>
        /// <param name="partitionKey">Partition key</param>
        /// <returns>The builder instance for fluent configuration</returns>
        public AzureServiceBusMessagePropertiesBuilder WithPartitionKey(string partitionKey)
        {
            _properties.PartitionKey = partitionKey;
            return this;
        }

        /// <summary>
        /// Sets the session identifier
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <returns>The builder instance for fluent configuration</returns>
        public AzureServiceBusMessagePropertiesBuilder WithSessionId(string sessionId)
        {
            _properties.SessionId = sessionId;
            return this;
        }

        /// <summary>
        /// Sets the reply-to address
        /// </summary>
        /// <param name="replyTo">Reply-to address</param>
        /// <returns>The builder instance for fluent configuration</returns>
        public AzureServiceBusMessagePropertiesBuilder WithReplyTo(string replyTo)
        {
            _properties.ReplyTo = replyTo;
            return this;
        }

        /// <summary>
        /// Sets the reply-to session identifier
        /// </summary>
        /// <param name="replyToSessionId">Reply-to session ID</param>
        /// <returns>The builder instance for fluent configuration</returns>
        public AzureServiceBusMessagePropertiesBuilder WithReplyToSessionId(string replyToSessionId)
        {
            _properties.ReplyToSessionId = replyToSessionId;
            return this;
        }

        /// <summary>
        /// Sets the time to live for the message
        /// </summary>
        /// <param name="timeToLive">Time to live</param>
        /// <returns>The builder instance for fluent configuration</returns>
        public AzureServiceBusMessagePropertiesBuilder WithTimeToLive(TimeSpan timeToLive)
        {
            _properties.TimeToLive = timeToLive;
            return this;
        }

        /// <summary>
        /// Sets the scheduled enqueue time (for delayed messages)
        /// </summary>
        /// <param name="scheduledEnqueueTime">Scheduled enqueue time</param>
        /// <returns>The builder instance for fluent configuration</returns>
        public AzureServiceBusMessagePropertiesBuilder WithScheduledEnqueueTime(DateTimeOffset scheduledEnqueueTime)
        {
            _properties.ScheduledEnqueueTime = scheduledEnqueueTime;
            return this;
        }

        /// <summary>
        /// Sets the subject/label for the message
        /// </summary>
        /// <param name="subject">Subject/label</param>
        /// <returns>The builder instance for fluent configuration</returns>
        public AzureServiceBusMessagePropertiesBuilder WithSubject(string subject)
        {
            _properties.Subject = subject;
            return this;
        }

        /// <summary>
        /// Sets the "to" address
        /// </summary>
        /// <param name="to">"To" address</param>
        /// <returns>The builder instance for fluent configuration</returns>
        public AzureServiceBusMessagePropertiesBuilder WithTo(string to)
        {
            _properties.To = to;
            return this;
        }

        /// <summary>
        /// Adds a custom property
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        /// <returns>The builder instance for fluent configuration</returns>
        public AzureServiceBusMessagePropertiesBuilder AddCustomProperty(string key, object value)
        {
            _properties.AddCustomProperty(key, value);
            return this;
        }

        /// <summary>
        /// Adds multiple custom properties
        /// </summary>
        /// <param name="properties">Dictionary of properties to add</param>
        /// <returns>The builder instance for fluent configuration</returns>
        public AzureServiceBusMessagePropertiesBuilder AddCustomProperties(Dictionary<string, object> properties)
        {
            _properties.AddCustomProperties(properties);
            return this;
        }

        /// <summary>
        /// Builds the message properties
        /// </summary>
        /// <returns>The configured message properties</returns>
        public AzureServiceBusMessageProperties Build()
        {
            return _properties;
        }

        /// <summary>
        /// Creates a new builder instance
        /// </summary>
        /// <returns>A new builder instance</returns>
        public static AzureServiceBusMessagePropertiesBuilder Create()
        {
            return new AzureServiceBusMessagePropertiesBuilder();
        }
    }
}

