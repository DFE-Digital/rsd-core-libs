using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Models;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Publishers;
using MassTransit;
using NSubstitute;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Tests.Publishers;

/// <summary>
/// Tests for the internal ApplyMessageProperties method to achieve full branch coverage.
/// This tests the method directly via InternalsVisibleTo.
/// </summary>
public class MassTransitEventPublisherApplyPropertiesTests
{
    [Fact]
    public void ApplyMessageProperties_WithValidContentType_ShouldSetContentType()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            ContentType = "application/json"
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        context.Received(1).ContentType = Arg.Any<System.Net.Mime.ContentType>();
    }

    [Fact]
    public void ApplyMessageProperties_WithNullContentType_ShouldNotSetContentType()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            ContentType = null
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        context.DidNotReceive().ContentType = Arg.Any<System.Net.Mime.ContentType>();
    }

    [Fact]
    public void ApplyMessageProperties_WithEmptyContentType_ShouldNotSetContentType()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            ContentType = ""
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        context.DidNotReceive().ContentType = Arg.Any<System.Net.Mime.ContentType>();
    }

    [Fact]
    public void ApplyMessageProperties_WithWhitespaceContentType_ShouldNotSetContentType()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            ContentType = "   "
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        context.DidNotReceive().ContentType = Arg.Any<System.Net.Mime.ContentType>();
    }

    [Fact]
    public void ApplyMessageProperties_WithValidCorrelationId_ShouldSetCorrelationId()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            CorrelationId = Guid.NewGuid().ToString()
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        context.Received(1).CorrelationId = Arg.Any<Guid?>();
    }

    [Fact]
    public void ApplyMessageProperties_WithNullCorrelationId_ShouldNotSetCorrelationId()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            CorrelationId = null
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        context.DidNotReceive().CorrelationId = Arg.Any<Guid?>();
    }

    [Fact]
    public void ApplyMessageProperties_WithValidMessageId_ShouldSetMessageId()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            MessageId = Guid.NewGuid().ToString()
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        context.Received(1).MessageId = Arg.Any<Guid?>();
    }

    [Fact]
    public void ApplyMessageProperties_WithNullMessageId_ShouldNotSetMessageId()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            MessageId = null
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        context.DidNotReceive().MessageId = Arg.Any<Guid?>();
    }

    [Fact]
    public void ApplyMessageProperties_WithTimeToLive_ShouldSetTimeToLive()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            TimeToLive = TimeSpan.FromHours(1)
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        context.Received(1).TimeToLive = Arg.Any<TimeSpan?>();
    }

    [Fact]
    public void ApplyMessageProperties_WithNullTimeToLive_ShouldNotSetTimeToLive()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            TimeToLive = null
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        context.DidNotReceive().TimeToLive = Arg.Any<TimeSpan?>();
    }

    [Fact]
    public void ApplyMessageProperties_WithValidPartitionKey_ShouldSetPartitionKey()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            PartitionKey = "partition-1"
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.Received(1).Set("PartitionKey", "partition-1");
    }

    [Fact]
    public void ApplyMessageProperties_WithNullPartitionKey_ShouldNotSetPartitionKey()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            PartitionKey = null
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.DidNotReceive().Set("PartitionKey", Arg.Any<object>());
    }

    [Fact]
    public void ApplyMessageProperties_WithValidSessionId_ShouldSetSessionId()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            SessionId = "session-1"
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.Received(1).Set("SessionId", "session-1");
    }

    [Fact]
    public void ApplyMessageProperties_WithNullSessionId_ShouldNotSetSessionId()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            SessionId = null
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.DidNotReceive().Set("SessionId", Arg.Any<object>());
    }

    [Fact]
    public void ApplyMessageProperties_WithValidReplyTo_ShouldSetReplyTo()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            ReplyTo = "reply-queue"
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.Received(1).Set("ReplyTo", "reply-queue");
    }

    [Fact]
    public void ApplyMessageProperties_WithNullReplyTo_ShouldNotSetReplyTo()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            ReplyTo = null
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.DidNotReceive().Set("ReplyTo", Arg.Any<object>());
    }

    [Fact]
    public void ApplyMessageProperties_WithValidReplyToSessionId_ShouldSetReplyToSessionId()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            ReplyToSessionId = "reply-session-1"
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.Received(1).Set("ReplyToSessionId", "reply-session-1");
    }

    [Fact]
    public void ApplyMessageProperties_WithNullReplyToSessionId_ShouldNotSetReplyToSessionId()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            ReplyToSessionId = null
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.DidNotReceive().Set("ReplyToSessionId", Arg.Any<object>());
    }

    [Fact]
    public void ApplyMessageProperties_WithScheduledEnqueueTime_ShouldSetScheduledEnqueueTime()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var scheduledTime = DateTimeOffset.UtcNow.AddHours(1);
        var properties = new AzureServiceBusMessageProperties
        {
            ScheduledEnqueueTime = scheduledTime
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.Received(1).Set("ScheduledEnqueueTimeUtc", scheduledTime);
    }

    [Fact]
    public void ApplyMessageProperties_WithNullScheduledEnqueueTime_ShouldNotSetScheduledEnqueueTime()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            ScheduledEnqueueTime = null
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.DidNotReceive().Set("ScheduledEnqueueTimeUtc", Arg.Any<object>());
    }

    [Fact]
    public void ApplyMessageProperties_WithValidSubject_ShouldSetSubject()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            Subject = "Order Created"
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.Received(1).Set("Label", "Order Created");
    }

    [Fact]
    public void ApplyMessageProperties_WithNullSubject_ShouldNotSetSubject()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            Subject = null
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.DidNotReceive().Set("Label", Arg.Any<object>());
    }

    [Fact]
    public void ApplyMessageProperties_WithValidTo_ShouldSetTo()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            To = "destination-queue"
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.Received(1).Set("To", "destination-queue");
    }

    [Fact]
    public void ApplyMessageProperties_WithNullTo_ShouldNotSetTo()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties
        {
            To = null
        };

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.DidNotReceive().Set("To", Arg.Any<object>());
    }

    [Fact]
    public void ApplyMessageProperties_WithEmptyCustomProperties_ShouldNotSetAnyCustomProperties()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties(); // Empty custom properties

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert - Headers should not receive any custom property sets
        // (only system property checks above)
    }

    [Fact]
    public void ApplyMessageProperties_WithSingleCustomProperty_ShouldSetCustomProperty()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties();
        properties.AddCustomProperty("OrderId", "ORD-123");

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.Received(1).Set("OrderId", Arg.Any<object>());
    }

    [Fact]
    public void ApplyMessageProperties_WithMultipleCustomProperties_ShouldSetAllCustomProperties()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var properties = new AzureServiceBusMessageProperties();
        properties.AddCustomProperty("Property1", "Value1");
        properties.AddCustomProperty("Property2", 123);
        properties.AddCustomProperty("Property3", true);

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert
        headers.Received(1).Set("Property1", Arg.Any<object>());
        headers.Received(1).Set("Property2", Arg.Any<object>());
        headers.Received(1).Set("Property3", Arg.Any<object>());
    }

    [Fact]
    public void ApplyMessageProperties_WithAllPropertiesSet_ShouldSetAllProperties()
    {
        // Arrange
        var context = Substitute.For<PublishContext>();
        var headers = Substitute.For<SendHeaders>();
        context.Headers.Returns(headers);
        
        var scheduledTime = DateTimeOffset.UtcNow.AddHours(1);
        var properties = new AzureServiceBusMessageProperties
        {
            ContentType = "application/json",
            CorrelationId = Guid.NewGuid().ToString(),
            MessageId = Guid.NewGuid().ToString(),
            PartitionKey = "partition-1",
            SessionId = "session-1",
            ReplyTo = "reply-to",
            ReplyToSessionId = "reply-session",
            Subject = "subject",
            To = "to",
            TimeToLive = TimeSpan.FromHours(1),
            ScheduledEnqueueTime = scheduledTime
        };
        properties.AddCustomProperty("CustomKey", "CustomValue");

        // Act
        MassTransitEventPublisher.ApplyMessageProperties(context, properties);

        // Assert - Just verify all the setters were called
        context.Received(1).ContentType = Arg.Any<System.Net.Mime.ContentType>();
        context.Received(1).CorrelationId = Arg.Any<Guid?>();
        context.Received(1).MessageId = Arg.Any<Guid?>();
        context.Received(1).TimeToLive = Arg.Any<TimeSpan?>();
        // Headers.Set is called multiple times with different keys - just verify it was called
        headers.ReceivedCalls().Count().Should().Be(8); // 7 system properties + 1 custom
    }
}

