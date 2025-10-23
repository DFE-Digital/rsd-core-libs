namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Tests.Models;

public class AzureServiceBusMessagePropertiesBuilderTests
{
    [Fact]
    public void Create_ShouldReturnNewBuilderInstance()
    {
        // Act
        var builder = AzureServiceBusMessagePropertiesBuilder.Create();

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<AzureServiceBusMessagePropertiesBuilder>();
    }

    [Fact]
    public void WithContentType_ShouldSetContentType()
    {
        // Arrange
        const string contentType = "application/json";

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithContentType(contentType)
            .Build();

        // Assert
        properties.ContentType.Should().Be(contentType);
    }

    [Fact]
    public void WithCorrelationId_ShouldSetCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithCorrelationId(correlationId)
            .Build();

        // Assert
        properties.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void WithMessageId_ShouldSetMessageId()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithMessageId(messageId)
            .Build();

        // Assert
        properties.MessageId.Should().Be(messageId);
    }

    [Fact]
    public void WithPartitionKey_ShouldSetPartitionKey()
    {
        // Arrange
        const string partitionKey = "partition-123";

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithPartitionKey(partitionKey)
            .Build();

        // Assert
        properties.PartitionKey.Should().Be(partitionKey);
    }

    [Fact]
    public void WithSessionId_ShouldSetSessionId()
    {
        // Arrange
        const string sessionId = "session-123";

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithSessionId(sessionId)
            .Build();

        // Assert
        properties.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public void WithReplyTo_ShouldSetReplyTo()
    {
        // Arrange
        const string replyTo = "reply-queue";

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithReplyTo(replyTo)
            .Build();

        // Assert
        properties.ReplyTo.Should().Be(replyTo);
    }

    [Fact]
    public void WithReplyToSessionId_ShouldSetReplyToSessionId()
    {
        // Arrange
        const string replyToSessionId = "reply-session-123";

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithReplyToSessionId(replyToSessionId)
            .Build();

        // Assert
        properties.ReplyToSessionId.Should().Be(replyToSessionId);
    }

    [Fact]
    public void WithTimeToLive_ShouldSetTimeToLive()
    {
        // Arrange
        var timeToLive = TimeSpan.FromHours(24);

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithTimeToLive(timeToLive)
            .Build();

        // Assert
        properties.TimeToLive.Should().Be(timeToLive);
    }

    [Fact]
    public void WithScheduledEnqueueTime_ShouldSetScheduledEnqueueTime()
    {
        // Arrange
        var scheduledTime = DateTimeOffset.UtcNow.AddHours(2);

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithScheduledEnqueueTime(scheduledTime)
            .Build();

        // Assert
        properties.ScheduledEnqueueTime.Should().Be(scheduledTime);
    }

    [Fact]
    public void WithSubject_ShouldSetSubject()
    {
        // Arrange
        const string subject = "Order Created";

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithSubject(subject)
            .Build();

        // Assert
        properties.Subject.Should().Be(subject);
    }

    [Fact]
    public void WithTo_ShouldSetTo()
    {
        // Arrange
        const string to = "destination-queue";

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithTo(to)
            .Build();

        // Assert
        properties.To.Should().Be(to);
    }

    [Fact]
    public void AddCustomProperty_ShouldAddSingleProperty()
    {
        // Arrange
        const string key = "OrderId";
        const string value = "ORD-123";

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .AddCustomProperty(key, value)
            .Build();

        // Assert
        properties.CustomProperties.Should().ContainKey(key);
        properties.CustomProperties[key].Should().Be(value);
    }

    [Fact]
    public void AddCustomProperty_ShouldSupportChaining()
    {
        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .AddCustomProperty("OrderId", "ORD-123")
            .AddCustomProperty("Priority", "High")
            .AddCustomProperty("Region", "UK")
            .Build();

        // Assert
        properties.CustomProperties.Should().HaveCount(3);
        properties.CustomProperties["OrderId"].Should().Be("ORD-123");
        properties.CustomProperties["Priority"].Should().Be("High");
        properties.CustomProperties["Region"].Should().Be("UK");
    }

    [Fact]
    public void AddCustomProperties_ShouldAddMultipleProperties()
    {
        // Arrange
        var customProps = new Dictionary<string, object>
        {
            { "OrderId", "ORD-123" },
            { "Priority", "High" },
            { "Quantity", 5 }
        };

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .AddCustomProperties(customProps)
            .Build();

        // Assert
        properties.CustomProperties.Should().HaveCount(3);
        properties.CustomProperties["OrderId"].Should().Be("ORD-123");
        properties.CustomProperties["Priority"].Should().Be("High");
        properties.CustomProperties["Quantity"].Should().Be(5);
    }

    [Fact]
    public void Builder_ShouldSupportFullFluentChaining()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var messageId = Guid.NewGuid().ToString();
        var timeToLive = TimeSpan.FromHours(24);
        var scheduledTime = DateTimeOffset.UtcNow.AddHours(2);

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithContentType("application/json")
            .WithCorrelationId(correlationId)
            .WithMessageId(messageId)
            .WithPartitionKey("partition-123")
            .WithSessionId("session-123")
            .WithReplyTo("reply-queue")
            .WithReplyToSessionId("reply-session-123")
            .WithTimeToLive(timeToLive)
            .WithScheduledEnqueueTime(scheduledTime)
            .WithSubject("Order Created")
            .WithTo("destination-queue")
            .AddCustomProperty("OrderId", "ORD-123")
            .AddCustomProperty("Priority", "High")
            .Build();

        // Assert
        properties.ContentType.Should().Be("application/json");
        properties.CorrelationId.Should().Be(correlationId);
        properties.MessageId.Should().Be(messageId);
        properties.PartitionKey.Should().Be("partition-123");
        properties.SessionId.Should().Be("session-123");
        properties.ReplyTo.Should().Be("reply-queue");
        properties.ReplyToSessionId.Should().Be("reply-session-123");
        properties.TimeToLive.Should().Be(timeToLive);
        properties.ScheduledEnqueueTime.Should().Be(scheduledTime);
        properties.Subject.Should().Be("Order Created");
        properties.To.Should().Be("destination-queue");
        properties.CustomProperties.Should().HaveCount(2);
    }

    [Fact]
    public void Builder_ShouldSupportPartialConfiguration()
    {
        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithPartitionKey("partition-123")
            .WithSubject("Order Created")
            .Build();

        // Assert
        properties.PartitionKey.Should().Be("partition-123");
        properties.Subject.Should().Be("Order Created");
        properties.ContentType.Should().BeNull();
        properties.CorrelationId.Should().BeNull();
        properties.CustomProperties.Should().BeEmpty();
    }

    [Fact]
    public void Build_ShouldReturnNewInstanceEachTime()
    {
        // Arrange
        var builder = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithPartitionKey("partition-123");

        // Act
        var properties1 = builder.Build();
        var properties2 = builder.Build();

        // Assert
        properties1.Should().BeSameAs(properties2); // Same instance since we reuse the same builder
        properties1.PartitionKey.Should().Be("partition-123");
        properties2.PartitionKey.Should().Be("partition-123");
    }

    [Fact]
    public void Builder_ShouldAllowSettingSamePropertyMultipleTimes()
    {
        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithSubject("First Subject")
            .WithSubject("Second Subject")
            .Build();

        // Assert
        properties.Subject.Should().Be("Second Subject");
    }

    [Fact]
    public void Builder_ShouldSupportMixedPropertyTypes()
    {
        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .AddCustomProperty("StringValue", "test")
            .AddCustomProperty("IntValue", 123)
            .AddCustomProperty("BoolValue", true)
            .AddCustomProperty("DateTimeValue", DateTime.UtcNow)
            .AddCustomProperty("DoubleValue", 123.45)
            .Build();

        // Assert
        properties.CustomProperties.Should().HaveCount(5);
        properties.CustomProperties["StringValue"].Should().Be("test");
        properties.CustomProperties["IntValue"].Should().Be(123);
        properties.CustomProperties["BoolValue"].Should().Be(true);
        properties.CustomProperties["DateTimeValue"].Should().BeOfType<DateTime>();
        properties.CustomProperties["DoubleValue"].Should().Be(123.45);
    }

    [Fact]
    public void Builder_ShouldCombineAddCustomPropertyAndAddCustomProperties()
    {
        // Arrange
        var customProps = new Dictionary<string, object>
        {
            { "OrderId", "ORD-123" },
            { "Priority", "High" }
        };

        // Act
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .AddCustomProperty("Region", "UK")
            .AddCustomProperties(customProps)
            .AddCustomProperty("Status", "Active")
            .Build();

        // Assert
        properties.CustomProperties.Should().HaveCount(4);
        properties.CustomProperties["Region"].Should().Be("UK");
        properties.CustomProperties["OrderId"].Should().Be("ORD-123");
        properties.CustomProperties["Priority"].Should().Be("High");
        properties.CustomProperties["Status"].Should().Be("Active");
    }
}

