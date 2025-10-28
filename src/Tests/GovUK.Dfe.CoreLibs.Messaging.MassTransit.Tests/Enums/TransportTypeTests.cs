using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Enums;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Tests.Enums;

public class TransportTypeTests
{
    [Fact]
    public void TransportType_ShouldHaveAzureServiceBusValue()
    {
        // Act
        var transportType = TransportType.AzureServiceBus;

        // Assert
        transportType.Should().BeDefined();
        Enum.IsDefined(typeof(TransportType), transportType).Should().BeTrue();
    }

    [Fact]
    public void TransportType_ShouldBeEnum()
    {
        // Assert
        typeof(TransportType).IsEnum.Should().BeTrue();
    }

    [Fact]
    public void TransportType_AzureServiceBus_ShouldHaveCorrectName()
    {
        // Act
        var name = TransportType.AzureServiceBus.ToString();

        // Assert
        name.Should().Be("AzureServiceBus");
    }

    [Fact]
    public void TransportType_ShouldHaveOneValue()
    {
        // Act
        var values = Enum.GetValues(typeof(TransportType));

        // Assert
        values.Length.Should().Be(1);
    }

    [Fact]
    public void TransportType_ShouldBeParseable()
    {
        // Act
        var parsed = Enum.Parse<TransportType>("AzureServiceBus");

        // Assert
        parsed.Should().Be(TransportType.AzureServiceBus);
    }

    [Fact]
    public void TransportType_InvalidValue_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Enum.Parse<TransportType>("InvalidTransport"));
    }
}

