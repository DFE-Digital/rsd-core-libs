using MassTransit;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Tests.Publishers;

public class MassTransitEventPublisherTests
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly MassTransitEventPublisher _publisher;

    public MassTransitEventPublisherTests()
    {
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _publisher = new MassTransitEventPublisher(_publishEndpoint);
    }

    #region PublishAsync Without Properties

    [Fact]
    public async Task PublishAsync_WithoutProperties_ShouldCallPublishEndpoint()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var cancellationToken = new CancellationToken();

        // Act
        await _publisher.PublishAsync(testEvent, cancellationToken);

        // Assert
        await _publishEndpoint.Received(1).Publish(testEvent, cancellationToken);
    }

    [Fact]
    public async Task PublishAsync_WithoutProperties_ShouldUseDefaultCancellationToken()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        await _publisher.PublishAsync(testEvent);

        // Assert
        await _publishEndpoint.Received(1).Publish(testEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithoutProperties_ShouldHandleNullableProperties()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = null };

        // Act
        await _publisher.PublishAsync(testEvent);

        // Assert
        await _publishEndpoint.Received(1).Publish(testEvent, Arg.Any<CancellationToken>());
    }

    #endregion

    #region PublishAsync With Properties

    [Fact]
    public async Task PublishAsync_WithProperties_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var properties = new AzureServiceBusMessageProperties
        {
            Subject = "Test Subject"
        };

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithContentType_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var properties = new AzureServiceBusMessageProperties
        {
            ContentType = "application/json"
        };

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithCorrelationId_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var correlationId = Guid.NewGuid().ToString();
        var properties = new AzureServiceBusMessageProperties
        {
            CorrelationId = correlationId
        };

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithMessageId_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var messageId = Guid.NewGuid().ToString();
        var properties = new AzureServiceBusMessageProperties
        {
            MessageId = messageId
        };

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithTimeToLive_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var properties = new AzureServiceBusMessageProperties
        {
            TimeToLive = TimeSpan.FromHours(24)
        };

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithPartitionKey_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var properties = new AzureServiceBusMessageProperties
        {
            PartitionKey = "partition-123"
        };

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithSessionId_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var properties = new AzureServiceBusMessageProperties
        {
            SessionId = "session-123"
        };

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithCustomProperties_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var properties = new AzureServiceBusMessageProperties();
        properties.AddCustomProperty("OrderId", "ORD-123");
        properties.AddCustomProperty("Priority", "High");

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithAllProperties_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var properties = AzureServiceBusMessagePropertiesBuilder
            .Create()
            .WithContentType("application/json")
            .WithCorrelationId(Guid.NewGuid().ToString())
            .WithMessageId(Guid.NewGuid().ToString())
            .WithPartitionKey("partition-123")
            .WithSessionId("session-123")
            .WithReplyTo("reply-queue")
            .WithReplyToSessionId("reply-session-123")
            .WithTimeToLive(TimeSpan.FromHours(24))
            .WithScheduledEnqueueTime(DateTimeOffset.UtcNow.AddHours(2))
            .WithSubject("Test Subject")
            .WithTo("destination-queue")
            .AddCustomProperty("OrderId", "ORD-123")
            .AddCustomProperty("Priority", "High")
            .Build();

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithEmptyProperties_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var properties = new AzureServiceBusMessageProperties();

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithNullStringProperties_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var properties = new AzureServiceBusMessageProperties
        {
            ContentType = null,
            CorrelationId = null,
            MessageId = null,
            PartitionKey = null
        };

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithEmptyStringProperties_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var properties = new AzureServiceBusMessageProperties
        {
            ContentType = "",
            CorrelationId = "   ",
            PartitionKey = ""
        };

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithScheduledEnqueueTime_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var properties = new AzureServiceBusMessageProperties
        {
            ScheduledEnqueueTime = DateTimeOffset.UtcNow.AddHours(2)
        };

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithReplyTo_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var properties = new AzureServiceBusMessageProperties
        {
            ReplyTo = "reply-queue",
            ReplyToSessionId = "reply-session-123"
        };

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithSubject_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var properties = new AzureServiceBusMessageProperties
        {
            Subject = "Order Created"
        };

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithTo_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = "Test" };
        var properties = new AzureServiceBusMessageProperties
        {
            To = "destination-queue"
        };

        // Act & Assert
        var act = async () => await _publisher.PublishAsync(testEvent, properties);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Helper Classes

    public class TestEvent
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }

    #endregion
}
