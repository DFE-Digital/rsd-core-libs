namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Tests.Models;

public class AzureServiceBusMessagePropertiesTests
{
    private readonly IFixture _fixture;

    public AzureServiceBusMessagePropertiesTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var properties = new AzureServiceBusMessageProperties();

        // Assert
        properties.ContentType.Should().BeNull();
        properties.CorrelationId.Should().BeNull();
        properties.MessageId.Should().BeNull();
        properties.PartitionKey.Should().BeNull();
        properties.SessionId.Should().BeNull();
        properties.ReplyTo.Should().BeNull();
        properties.ReplyToSessionId.Should().BeNull();
        properties.TimeToLive.Should().BeNull();
        properties.ScheduledEnqueueTime.Should().BeNull();
        properties.Subject.Should().BeNull();
        properties.To.Should().BeNull();
        properties.CustomProperties.Should().NotBeNull();
        properties.CustomProperties.Should().BeEmpty();
    }

    [Fact]
    public void ContentType_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        const string contentType = "application/json";

        // Act
        properties.ContentType = contentType;

        // Assert
        properties.ContentType.Should().Be(contentType);
    }

    [Fact]
    public void CorrelationId_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        var correlationId = Guid.NewGuid().ToString();

        // Act
        properties.CorrelationId = correlationId;

        // Assert
        properties.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void MessageId_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        var messageId = Guid.NewGuid().ToString();

        // Act
        properties.MessageId = messageId;

        // Assert
        properties.MessageId.Should().Be(messageId);
    }

    [Fact]
    public void PartitionKey_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        const string partitionKey = "partition-123";

        // Act
        properties.PartitionKey = partitionKey;

        // Assert
        properties.PartitionKey.Should().Be(partitionKey);
    }

    [Fact]
    public void SessionId_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        const string sessionId = "session-123";

        // Act
        properties.SessionId = sessionId;

        // Assert
        properties.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public void ReplyTo_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        const string replyTo = "reply-queue";

        // Act
        properties.ReplyTo = replyTo;

        // Assert
        properties.ReplyTo.Should().Be(replyTo);
    }

    [Fact]
    public void ReplyToSessionId_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        const string replyToSessionId = "reply-session-123";

        // Act
        properties.ReplyToSessionId = replyToSessionId;

        // Assert
        properties.ReplyToSessionId.Should().Be(replyToSessionId);
    }

    [Fact]
    public void TimeToLive_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        var timeToLive = TimeSpan.FromHours(24);

        // Act
        properties.TimeToLive = timeToLive;

        // Assert
        properties.TimeToLive.Should().Be(timeToLive);
    }

    [Fact]
    public void ScheduledEnqueueTime_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        var scheduledTime = DateTimeOffset.UtcNow.AddHours(2);

        // Act
        properties.ScheduledEnqueueTime = scheduledTime;

        // Assert
        properties.ScheduledEnqueueTime.Should().Be(scheduledTime);
    }

    [Fact]
    public void Subject_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        const string subject = "Order Created";

        // Act
        properties.Subject = subject;

        // Assert
        properties.Subject.Should().Be(subject);
    }

    [Fact]
    public void To_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        const string to = "destination-queue";

        // Act
        properties.To = to;

        // Assert
        properties.To.Should().Be(to);
    }

    [Fact]
    public void AddCustomProperty_ShouldAddPropertyCorrectly()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        const string key = "OrderId";
        const string value = "ORD-123";

        // Act
        var result = properties.AddCustomProperty(key, value);

        // Assert
        result.Should().BeSameAs(properties); // Fluent API
        properties.CustomProperties.Should().ContainKey(key);
        properties.CustomProperties[key].Should().Be(value);
    }

    [Fact]
    public void AddCustomProperty_ShouldOverwriteExistingProperty()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        const string key = "Priority";
        const string value1 = "Low";
        const string value2 = "High";

        // Act
        properties.AddCustomProperty(key, value1);
        properties.AddCustomProperty(key, value2);

        // Assert
        properties.CustomProperties[key].Should().Be(value2);
    }

    [Fact]
    public void AddCustomProperty_ShouldSupportMultipleProperties()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();

        // Act
        properties
            .AddCustomProperty("OrderId", "ORD-123")
            .AddCustomProperty("Priority", "High")
            .AddCustomProperty("Region", "UK");

        // Assert
        properties.CustomProperties.Should().HaveCount(3);
        properties.CustomProperties["OrderId"].Should().Be("ORD-123");
        properties.CustomProperties["Priority"].Should().Be("High");
        properties.CustomProperties["Region"].Should().Be("UK");
    }

    [Fact]
    public void AddCustomProperty_ShouldSupportDifferentValueTypes()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        var dateTime = DateTime.UtcNow;

        // Act
        properties
            .AddCustomProperty("StringValue", "test")
            .AddCustomProperty("IntValue", 123)
            .AddCustomProperty("BoolValue", true)
            .AddCustomProperty("DateTimeValue", dateTime)
            .AddCustomProperty("DoubleValue", 123.45);

        // Assert
        properties.CustomProperties["StringValue"].Should().Be("test");
        properties.CustomProperties["IntValue"].Should().Be(123);
        properties.CustomProperties["BoolValue"].Should().Be(true);
        properties.CustomProperties["DateTimeValue"].Should().Be(dateTime);
        properties.CustomProperties["DoubleValue"].Should().Be(123.45);
    }

    [Fact]
    public void AddCustomProperties_ShouldAddMultiplePropertiesFromDictionary()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        var customProps = new Dictionary<string, object>
        {
            { "OrderId", "ORD-123" },
            { "Priority", "High" },
            { "Quantity", 5 }
        };

        // Act
        var result = properties.AddCustomProperties(customProps);

        // Assert
        result.Should().BeSameAs(properties); // Fluent API
        properties.CustomProperties.Should().HaveCount(3);
        properties.CustomProperties["OrderId"].Should().Be("ORD-123");
        properties.CustomProperties["Priority"].Should().Be("High");
        properties.CustomProperties["Quantity"].Should().Be(5);
    }

    [Fact]
    public void AddCustomProperties_ShouldOverwriteExistingProperties()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        properties.AddCustomProperty("Priority", "Low");

        var customProps = new Dictionary<string, object>
        {
            { "Priority", "High" },
            { "OrderId", "ORD-123" }
        };

        // Act
        properties.AddCustomProperties(customProps);

        // Assert
        properties.CustomProperties.Should().HaveCount(2);
        properties.CustomProperties["Priority"].Should().Be("High");
        properties.CustomProperties["OrderId"].Should().Be("ORD-123");
    }

    [Fact]
    public void AddCustomProperties_ShouldHandleEmptyDictionary()
    {
        // Arrange
        var properties = new AzureServiceBusMessageProperties();
        var customProps = new Dictionary<string, object>();

        // Act
        properties.AddCustomProperties(customProps);

        // Assert
        properties.CustomProperties.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldSupportFullConfiguration()
    {
        // Arrange & Act
        var properties = new AzureServiceBusMessageProperties
        {
            ContentType = "application/json",
            CorrelationId = Guid.NewGuid().ToString(),
            MessageId = Guid.NewGuid().ToString(),
            PartitionKey = "partition-123",
            SessionId = "session-123",
            ReplyTo = "reply-queue",
            ReplyToSessionId = "reply-session-123",
            TimeToLive = TimeSpan.FromHours(24),
            ScheduledEnqueueTime = DateTimeOffset.UtcNow.AddHours(2),
            Subject = "Order Created",
            To = "destination-queue"
        };

        properties
            .AddCustomProperty("OrderId", "ORD-123")
            .AddCustomProperty("Priority", "High");

        // Assert
        properties.ContentType.Should().Be("application/json");
        properties.CorrelationId.Should().NotBeNullOrEmpty();
        properties.MessageId.Should().NotBeNullOrEmpty();
        properties.PartitionKey.Should().Be("partition-123");
        properties.SessionId.Should().Be("session-123");
        properties.ReplyTo.Should().Be("reply-queue");
        properties.ReplyToSessionId.Should().Be("reply-session-123");
        properties.TimeToLive.Should().Be(TimeSpan.FromHours(24));
        properties.ScheduledEnqueueTime.Should().NotBeNull();
        properties.Subject.Should().Be("Order Created");
        properties.To.Should().Be("destination-queue");
        properties.CustomProperties.Should().HaveCount(2);
    }
}

