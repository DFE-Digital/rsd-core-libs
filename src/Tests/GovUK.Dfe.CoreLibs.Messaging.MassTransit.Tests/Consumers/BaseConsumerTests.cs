using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Tests.Consumers;

public class BaseConsumerTests
{
    private readonly ILogger<TestConsumer> _logger;

    public BaseConsumerTests()
    {
        _logger = Substitute.For<ILogger<TestConsumer>>();
    }

    [Fact]
    public async Task Consume_WithValidMessage_ShouldCallHandleMessageAsync()
    {
        // Arrange
        var consumer = new TestConsumer(_logger);
        var context = Substitute.For<ConsumeContext<TestMessage>>();
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Content = "Test content" };
        context.Message.Returns(testMessage);

        // Act
        await consumer.Consume(context);

        // Assert
        consumer.HandledMessages.Should().ContainSingle();
        consumer.HandledMessages.First().Should().Be(testMessage);
    }

    [Fact]
    public async Task Consume_WhenHandleMessageAsyncThrowsException_ShouldLogError()
    {
        // Arrange
        var consumer = new ThrowingConsumer(_logger);
        var context = Substitute.For<ConsumeContext<TestMessage>>();
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Content = "Test content" };
        context.Message.Returns(testMessage);

        // Act
        var act = async () => await consumer.Consume(context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("Error processing message of type")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()!);
    }

    [Fact]
    public async Task Consume_WhenHandleMessageAsyncThrows_ShouldIncludeMessageTypeInLog()
    {
        // Arrange
        var consumer = new ThrowingConsumer(_logger);
        var context = Substitute.For<ConsumeContext<TestMessage>>();
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Content = "Test content" };
        context.Message.Returns(testMessage);

        // Act
        try
        {
            await consumer.Consume(context);
        }
        catch
        {
            // Expected
        }

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains(nameof(TestMessage))),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()!);
    }

    [Fact]
    public async Task Consume_WithMultipleMessages_ShouldHandleEachMessage()
    {
        // Arrange
        var consumer = new TestConsumer(_logger);
        var message1 = new TestMessage { Id = Guid.NewGuid(), Content = "Message 1" };
        var message2 = new TestMessage { Id = Guid.NewGuid(), Content = "Message 2" };
        var message3 = new TestMessage { Id = Guid.NewGuid(), Content = "Message 3" };

        var context1 = Substitute.For<ConsumeContext<TestMessage>>();
        var context2 = Substitute.For<ConsumeContext<TestMessage>>();
        var context3 = Substitute.For<ConsumeContext<TestMessage>>();

        context1.Message.Returns(message1);
        context2.Message.Returns(message2);
        context3.Message.Returns(message3);

        // Act
        await consumer.Consume(context1);
        await consumer.Consume(context2);
        await consumer.Consume(context3);

        // Assert
        consumer.HandledMessages.Should().HaveCount(3);
        consumer.HandledMessages.Should().Contain(message1);
        consumer.HandledMessages.Should().Contain(message2);
        consumer.HandledMessages.Should().Contain(message3);
    }

    [Fact]
    public async Task Consume_WithNullableProperties_ShouldHandleCorrectly()
    {
        // Arrange
        var consumer = new TestConsumer(_logger);
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Content = null };
        var context = Substitute.For<ConsumeContext<TestMessage>>();
        context.Message.Returns(testMessage);

        // Act
        await consumer.Consume(context);

        // Assert
        consumer.HandledMessages.Should().ContainSingle();
        consumer.HandledMessages.First().Content.Should().BeNull();
    }

    [Fact]
    public async Task Consume_ShouldPassConsumeContextToHandleMessage()
    {
        // Arrange
        var consumer = new ContextCheckingConsumer(_logger);
        var context = Substitute.For<ConsumeContext<TestMessage>>();
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Content = "Test" };
        var messageId = Guid.NewGuid();
        
        context.Message.Returns(testMessage);
        context.MessageId.Returns(messageId);

        // Act
        await consumer.Consume(context);

        // Assert
        consumer.CapturedContext.Should().NotBeNull();
        consumer.CapturedContext!.MessageId.Should().Be(messageId);
    }

    [Fact]
    public async Task Consume_WithSuccessfulHandling_ShouldNotLogError()
    {
        // Arrange
        var consumer = new TestConsumer(_logger);
        var context = Substitute.For<ConsumeContext<TestMessage>>();
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Content = "Test" };
        context.Message.Returns(testMessage);

        // Act
        await consumer.Consume(context);

        // Assert
        _logger.DidNotReceive().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()!);
    }

    [Fact]
    public async Task Consume_WithException_ShouldRethrowException()
    {
        // Arrange
        var consumer = new ThrowingConsumer(_logger);
        var context = Substitute.For<ConsumeContext<TestMessage>>();
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Content = "Test" };
        context.Message.Returns(testMessage);

        // Act
        var act = async () => await consumer.Consume(context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #region Helper Classes

    public class TestMessage
    {
        public Guid Id { get; set; }
        public string? Content { get; set; }
    }

    public class TestConsumer : BaseConsumer<TestMessage>
    {
        public List<TestMessage> HandledMessages { get; } = new();

        public TestConsumer(ILogger logger) : base(logger)
        {
        }

        protected override Task HandleMessageAsync(ConsumeContext<TestMessage> context)
        {
            HandledMessages.Add(context.Message);
            return Task.CompletedTask;
        }
    }

    public class ThrowingConsumer : BaseConsumer<TestMessage>
    {
        public ThrowingConsumer(ILogger logger) : base(logger)
        {
        }

        protected override Task HandleMessageAsync(ConsumeContext<TestMessage> context)
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    public class ContextCheckingConsumer : BaseConsumer<TestMessage>
    {
        public ConsumeContext<TestMessage>? CapturedContext { get; private set; }

        public ContextCheckingConsumer(ILogger logger) : base(logger)
        {
        }

        protected override Task HandleMessageAsync(ConsumeContext<TestMessage> context)
        {
            CapturedContext = context;
            return Task.CompletedTask;
        }
    }

    #endregion
}

