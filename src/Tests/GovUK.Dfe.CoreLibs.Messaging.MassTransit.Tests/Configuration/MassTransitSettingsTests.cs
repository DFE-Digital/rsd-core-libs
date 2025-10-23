using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Enums;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Tests.Configuration;

public class MassTransitSettingsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var settings = new MassTransitSettings();

        // Assert
        settings.Transport.Should().Be(TransportType.AzureServiceBus);
        settings.AppPrefix.Should().BeEmpty();
        settings.AzureServiceBus.Should().NotBeNull();
    }

    [Fact]
    public void Transport_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var settings = new MassTransitSettings();

        // Act
        settings.Transport = TransportType.AzureServiceBus;

        // Assert
        settings.Transport.Should().Be(TransportType.AzureServiceBus);
    }

    [Fact]
    public void AppPrefix_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var settings = new MassTransitSettings();
        const string appPrefix = "test-app";

        // Act
        settings.AppPrefix = appPrefix;

        // Assert
        settings.AppPrefix.Should().Be(appPrefix);
    }

    [Fact]
    public void AppPrefix_ShouldAcceptEmptyString()
    {
        // Arrange
        var settings = new MassTransitSettings();

        // Act
        settings.AppPrefix = "";

        // Assert
        settings.AppPrefix.Should().BeEmpty();
    }

    [Fact]
    public void AzureServiceBus_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var settings = new MassTransitSettings();
        var azureSettings = new AzureServiceBusSettings
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/"
        };

        // Act
        settings.AzureServiceBus = azureSettings;

        // Assert
        settings.AzureServiceBus.Should().BeSameAs(azureSettings);
        settings.AzureServiceBus.ConnectionString.Should().Be("Endpoint=sb://test.servicebus.windows.net/");
    }

    [Fact]
    public void Settings_ShouldSupportFullConfiguration()
    {
        // Act
        var settings = new MassTransitSettings
        {
            Transport = TransportType.AzureServiceBus,
            AppPrefix = "my-app",
            AzureServiceBus = new AzureServiceBusSettings
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey"
            }
        };

        // Assert
        settings.Transport.Should().Be(TransportType.AzureServiceBus);
        settings.AppPrefix.Should().Be("my-app");
        settings.AzureServiceBus.ConnectionString.Should().Contain("test.servicebus.windows.net");
    }

    [Fact]
    public void Settings_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = @"{
            ""Transport"": ""AzureServiceBus"",
            ""AppPrefix"": ""test-app"",
            ""AzureServiceBus"": {
                ""ConnectionString"": ""Endpoint=sb://test.servicebus.windows.net/""
            }
        }";

        // Act
        var settings = System.Text.Json.JsonSerializer.Deserialize<MassTransitSettings>(json);

        // Assert
        settings.Should().NotBeNull();
        settings!.Transport.Should().Be(TransportType.AzureServiceBus);
        settings.AppPrefix.Should().Be("test-app");
        settings.AzureServiceBus.ConnectionString.Should().Be("Endpoint=sb://test.servicebus.windows.net/");
    }
}

public class AzureServiceBusSettingsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var settings = new AzureServiceBusSettings();

        // Assert
        settings.ConnectionString.Should().BeEmpty();
        settings.AutoCreateEntities.Should().BeTrue(); // Default is true
    }

    [Fact]
    public void ConnectionString_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var settings = new AzureServiceBusSettings();
        const string connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";

        // Act
        settings.ConnectionString = connectionString;

        // Assert
        settings.ConnectionString.Should().Be(connectionString);
    }

    [Fact]
    public void ConnectionString_ShouldAcceptEmptyString()
    {
        // Arrange
        var settings = new AzureServiceBusSettings();

        // Act
        settings.ConnectionString = "";

        // Assert
        settings.ConnectionString.Should().BeEmpty();
    }

    [Fact]
    public void ConnectionString_ShouldSupportComplexConnectionString()
    {
        // Arrange
        var settings = new AzureServiceBusSettings();
        const string connectionString = "Endpoint=sb://my-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=abcdefghijklmnopqrstuvwxyz123456";

        // Act
        settings.ConnectionString = connectionString;

        // Assert
        settings.ConnectionString.Should().Be(connectionString);
        settings.ConnectionString.Should().Contain("my-namespace.servicebus.windows.net");
        settings.ConnectionString.Should().Contain("SharedAccessKeyName=RootManageSharedAccessKey");
    }

    [Fact]
    public void AutoCreateEntities_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var settings = new AzureServiceBusSettings();

        // Act
        settings.AutoCreateEntities = false;

        // Assert
        settings.AutoCreateEntities.Should().BeFalse();
    }

    [Fact]
    public void Settings_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = @"{
            ""ConnectionString"": ""Endpoint=sb://test.servicebus.windows.net/"",
            ""AutoCreateEntities"": false
        }";

        // Act
        var settings = System.Text.Json.JsonSerializer.Deserialize<AzureServiceBusSettings>(json);

        // Assert
        settings.Should().NotBeNull();
        settings!.ConnectionString.Should().Be("Endpoint=sb://test.servicebus.windows.net/");
        settings.AutoCreateEntities.Should().BeFalse();
    }

    [Fact]
    public void Settings_ShouldDeserializeFromJsonWithDefaultAutoCreateEntities()
    {
        // Arrange
        var json = @"{
            ""ConnectionString"": ""Endpoint=sb://test.servicebus.windows.net/""
        }";

        // Act
        var settings = System.Text.Json.JsonSerializer.Deserialize<AzureServiceBusSettings>(json);

        // Assert
        settings.Should().NotBeNull();
        settings!.ConnectionString.Should().Be("Endpoint=sb://test.servicebus.windows.net/");
        settings.AutoCreateEntities.Should().BeTrue(); // Default value
    }
}

public class TransportTypeTests
{
    [Fact]
    public void TransportType_ShouldHaveAzureServiceBusValue()
    {
        // Act
        var transportType = TransportType.AzureServiceBus;

        // Assert
        transportType.Should().Be(TransportType.AzureServiceBus);
        ((int)transportType).Should().Be(0);
    }

    [Fact]
    public void TransportType_ShouldConvertToString()
    {
        // Act
        var transportType = TransportType.AzureServiceBus;
        var transportString = transportType.ToString();

        // Assert
        transportString.Should().Be("AzureServiceBus");
    }

    [Fact]
    public void TransportType_ShouldParseFromString()
    {
        // Act
        var success = Enum.TryParse<TransportType>("AzureServiceBus", out var transportType);

        // Assert
        success.Should().BeTrue();
        transportType.Should().Be(TransportType.AzureServiceBus);
    }

    [Fact]
    public void TransportType_ShouldParseFromStringIgnoreCase()
    {
        // Act
        var success = Enum.TryParse<TransportType>("azureservicebus", true, out var transportType);

        // Assert
        success.Should().BeTrue();
        transportType.Should().Be(TransportType.AzureServiceBus);
    }
}

