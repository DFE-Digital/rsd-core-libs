using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Configuration;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Enums;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Tests.Helpers;

public class ServiceBusEntitySetupHostedServiceTests
{
    private readonly ILogger<ServiceBusEntitySetupHostedService> _logger;
    private readonly IOptions<MassTransitSettings> _options;

    public ServiceBusEntitySetupHostedServiceTests()
    {
        _logger = Substitute.For<ILogger<ServiceBusEntitySetupHostedService>>();
        _options = Substitute.For<IOptions<MassTransitSettings>>();
    }

    [Fact]
    public void ServiceBusEntitySetupHostedService_ShouldBeCreated()
    {
        // Arrange
        var settings = new MassTransitSettings
        {
            Transport = TransportType.AzureServiceBus,
            AzureServiceBus = new AzureServiceBusSettings
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey"
            }
        };
        _options.Value.Returns(settings);

        // Act
        var service = new ServiceBusEntitySetupHostedService(_options, _logger);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task StartAsync_WithInvalidConnectionString_ShouldThrowException()
    {
        // Arrange
        var settings = new MassTransitSettings
        {
            Transport = TransportType.AzureServiceBus,
            AzureServiceBus = new AzureServiceBusSettings
            {
                ConnectionString = "InvalidConnectionString"
            }
        };
        _options.Value.Returns(settings);

        var service = new ServiceBusEntitySetupHostedService(_options, _logger);
        var cancellationToken = new CancellationToken();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await service.StartAsync(cancellationToken));
    }

    [Fact]
    public async Task StopAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var settings = new MassTransitSettings
        {
            Transport = TransportType.AzureServiceBus,
            AzureServiceBus = new AzureServiceBusSettings
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey"
            }
        };
        _options.Value.Returns(settings);

        var service = new ServiceBusEntitySetupHostedService(_options, _logger);
        var cancellationToken = new CancellationToken();

        // Act
        await service.StopAsync(cancellationToken);

        // Assert - StopAsync should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task StartAsync_ShouldLogInformation()
    {
        // Arrange
        var settings = new MassTransitSettings
        {
            Transport = TransportType.AzureServiceBus,
            AzureServiceBus = new AzureServiceBusSettings
            {
                ConnectionString = "InvalidConnectionString" // Invalid to prevent actual connection
            }
        };
        _options.Value.Returns(settings);

        var service = new ServiceBusEntitySetupHostedService(_options, _logger);
        var cancellationToken = new CancellationToken();

        // Act
        try
        {
            await service.StartAsync(cancellationToken);
        }
        catch
        {
            // Expected to fail with invalid connection string
        }

        // Assert - verify logging was called
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Starting Service Bus entity setup")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task StartAsync_WithEmptyConnectionString_ShouldThrow()
    {
        // Arrange
        var settings = new MassTransitSettings
        {
            Transport = TransportType.AzureServiceBus,
            AzureServiceBus = new AzureServiceBusSettings
            {
                ConnectionString = ""
            }
        };
        _options.Value.Returns(settings);

        var service = new ServiceBusEntitySetupHostedService(_options, _logger);
        var cancellationToken = new CancellationToken();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await service.StartAsync(cancellationToken));
    }

    [Fact]
    public async Task StopAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var settings = new MassTransitSettings
        {
            Transport = TransportType.AzureServiceBus,
            AzureServiceBus = new AzureServiceBusSettings
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey"
            }
        };
        _options.Value.Returns(settings);

        var service = new ServiceBusEntitySetupHostedService(_options, _logger);
        var cts = new CancellationTokenSource();

        // Act
        await service.StopAsync(cts.Token);

        // Assert - Should not throw
        Assert.True(true);
    }
}

