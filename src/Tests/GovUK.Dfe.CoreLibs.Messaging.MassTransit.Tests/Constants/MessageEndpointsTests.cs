using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Constants;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Tests.Constants;

public class MessageEndpointsTests
{
    [Fact]
    public void TransferApplicationReceivedTopic_ShouldHaveCorrectValue()
    {
        // Act
        var topicName = MessageEndpoints.TransferApplicationReceivedTopic;

        // Assert
        topicName.Should().Be("transfer-application-received");
        topicName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ReportGenerationQueue_ShouldHaveCorrectValue()
    {
        // Act
        var queueName = MessageEndpoints.ReportGenerationQueue;

        // Assert
        queueName.Should().Be("report-generation");
        queueName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MessageEndpoints_ShouldBeStaticClass()
    {
        // Assert
        typeof(MessageEndpoints).Should().BeStatic();
    }

    [Fact]
    public void AllEndpoints_ShouldBeConstStrings()
    {
        // Assert
        var fields = typeof(MessageEndpoints).GetFields(
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Static);

        foreach (var field in fields)
        {
            field.IsLiteral.Should().BeTrue(); // const fields are literal
            field.FieldType.Should().Be(typeof(string));
        }
    }
}

