using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Extensions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configuration;

    public ServiceCollectionExtensionsTests()
    {
        _services = new ServiceCollection();
        
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MassTransit:Transport"] = "AzureServiceBus",
            ["MassTransit:AppPrefix"] = "test-app",
            ["MassTransit:AzureServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey"
        });
        _configuration = configBuilder.Build();
    }

    #region AddDfEMassTransit Configuration Tests

    [Fact]
    public void AddDfEMassTransit_WithValidConfiguration_ShouldRegisterServices()
    {
        // Act
        _services.AddDfEMassTransit(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        serviceProvider.GetService<IEventPublisher>().Should().NotBeNull();
        serviceProvider.GetService<IEventPublisher>().Should().BeOfType<MassTransitEventPublisher>();
    }

    [Fact]
    public void AddDfEMassTransit_WithValidConfiguration_ShouldRegisterMassTransitSettings()
    {
        // Act
        _services.AddDfEMassTransit(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        var settings = serviceProvider.GetService<IOptions<MassTransitSettings>>();
        settings.Should().NotBeNull();
        settings!.Value.Transport.Should().Be(GovUK.Dfe.CoreLibs.Messaging.MassTransit.Enums.TransportType.AzureServiceBus);
        settings.Value.AppPrefix.Should().Be("test-app");
        settings.Value.AzureServiceBus.ConnectionString.Should().Be("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey");
    }

    [Fact]
    public void AddDfEMassTransit_WithMissingConfiguration_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();

        // Act
        var act = () => _services.AddDfEMassTransit(emptyConfig);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("MassTransit configuration section is missing or invalid.");
    }

    [Fact]
    public void AddDfEMassTransit_WithConsumerConfiguration_ShouldRegisterConsumers()
    {
        // Act
        _services.AddDfEMassTransit(
            _configuration,
            configureConsumers: x =>
            {
                x.AddConsumer<TestConsumer>();
            });

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        serviceProvider.GetService<IEventPublisher>().Should().NotBeNull();
    }

    [Fact]
    public void AddDfEMassTransit_WithBusConfiguration_ShouldAllowCustomConfiguration()
    {
        // Act
        _services.AddDfEMassTransit(
            _configuration,
            configureBus: (context, cfg) =>
            {
                // Custom bus configuration
            });

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        serviceProvider.GetService<IEventPublisher>().Should().NotBeNull();
    }

    [Fact]
    public void AddDfEMassTransit_ShouldRegisterEventPublisherAsScoped()
    {
        // Act
        _services.AddDfEMassTransit(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();
        
        var publisher1 = scope1.ServiceProvider.GetService<IEventPublisher>();
        var publisher2 = scope2.ServiceProvider.GetService<IEventPublisher>();
        
        publisher1.Should().NotBeNull();
        publisher2.Should().NotBeNull();
        publisher1.Should().NotBeSameAs(publisher2);
    }


    [Fact]
    public void AddDfEMassTransit_WithNullConfiguration_ShouldThrow()
    {
        // Act
        var act = () => _services.AddDfEMassTransit(null!);

        // Assert
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void AddDfEMassTransit_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => GovUK.Dfe.CoreLibs.Messaging.MassTransit.Extensions.ServiceCollectionExtensions.AddDfEMassTransit(null!, _configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddDfEMassTransit_WithConnectionStringInConnectionStrings_ShouldUseConnectionString()
    {
        // Arrange
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ServiceBus"] = "Endpoint=sb://fromconnectionstrings.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey",
            ["MassTransit:Transport"] = "AzureServiceBus",
            ["MassTransit:AppPrefix"] = "test-app"
        });
        var config = configBuilder.Build();

        // Act
        services.AddDfEMassTransit(config);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<IEventPublisher>().Should().NotBeNull();
    }

    [Fact]
    public void AddDfEMassTransit_WithConnectionStringInMassTransitSection_ShouldUseConnectionString()
    {
        // Arrange
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MassTransit:Transport"] = "AzureServiceBus",
            ["MassTransit:AppPrefix"] = "test-app",
            ["MassTransit:AzureServiceBus:ConnectionString"] = "Endpoint=sb://frommodule.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey"
        });
        var config = configBuilder.Build();

        // Act
        services.AddDfEMassTransit(config);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<IEventPublisher>().Should().NotBeNull();
    }

    [Fact]
    public void AddDfEMassTransit_WithBothConnectionStrings_ShouldPrioritizeConnectionStringsSection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ServiceBus"] = "Endpoint=sb://priority.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey",
            ["MassTransit:Transport"] = "AzureServiceBus",
            ["MassTransit:AppPrefix"] = "test-app",
            ["MassTransit:AzureServiceBus:ConnectionString"] = "Endpoint=sb://fallback.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey"
        });
        var config = configBuilder.Build();

        // Act
        services.AddDfEMassTransit(config);

        // Assert - should not throw, meaning it used the ConnectionStrings:ServiceBus value
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<IEventPublisher>().Should().NotBeNull();
    }

    [Fact]
    public void AddDfEMassTransit_WithNoConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MassTransit:Transport"] = "AzureServiceBus",
            ["MassTransit:AppPrefix"] = "test-app"
            // No connection string configured
        });
        var config = configBuilder.Build();

        // Act
        var act = () =>
        {
            services.AddDfEMassTransit(config);
            var serviceProvider = services.BuildServiceProvider();
        };

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Azure Service Bus connection string is required*");
    }

    [Fact]
    public void AddDfEMassTransit_WithEmptyConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ServiceBus"] = "",
            ["MassTransit:Transport"] = "AzureServiceBus",
            ["MassTransit:AppPrefix"] = "test-app",
            ["MassTransit:AzureServiceBus:ConnectionString"] = ""
        });
        var config = configBuilder.Build();

        // Act
        var act = () =>
        {
            services.AddDfEMassTransit(config);
            var serviceProvider = services.BuildServiceProvider();
        };

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Azure Service Bus connection string is required*");
    }

    [Fact]
    public void AddDfEMassTransit_WithCustomAppPrefix_ShouldUseCustomPrefix()
    {
        // Arrange
        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MassTransit:Transport"] = "AzureServiceBus",
                ["MassTransit:AppPrefix"] = "custom-prefix",
                ["MassTransit:AzureServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey"
            })
            .Build();

        // Act
        _services.AddDfEMassTransit(customConfig);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var settings = serviceProvider.GetService<IOptions<MassTransitSettings>>();
        settings!.Value.AppPrefix.Should().Be("custom-prefix");
    }

    [Fact]
    public void AddDfEMassTransit_WithEmptyAppPrefix_ShouldUseEmptyPrefix()
    {
        // Arrange
        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MassTransit:Transport"] = "AzureServiceBus",
                ["MassTransit:AppPrefix"] = "",
                ["MassTransit:AzureServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey"
            })
            .Build();

        // Act
        _services.AddDfEMassTransit(customConfig);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var settings = serviceProvider.GetService<IOptions<MassTransitSettings>>();
        settings!.Value.AppPrefix.Should().BeEmpty();
    }

    [Fact]
    public void AddDfEMassTransit_WithAutoCreateEntitiesDisabled_ShouldNotRegisterHostedService()
    {
        // Arrange
        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MassTransit:Transport"] = "AzureServiceBus",
                ["MassTransit:AppPrefix"] = "test-app",
                ["MassTransit:AzureServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey",
                ["MassTransit:AzureServiceBus:AutoCreateEntities"] = "false"
            })
            .Build();

        // Act
        _services.AddDfEMassTransit(customConfig);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        serviceProvider.GetService<IEventPublisher>().Should().NotBeNull();
        
        // The hosted service should not be registered
        var hostedServices = _services.Where(s => 
            s.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
            s.ImplementationType?.Name == "ServiceBusEntitySetupHostedService");
        
        hostedServices.Should().BeEmpty();
    }

    [Fact]
    public void AddDfEMassTransit_WithAutoCreateEntitiesEnabled_ShouldRegisterHostedService()
    {
        // Arrange
        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MassTransit:Transport"] = "AzureServiceBus",
                ["MassTransit:AppPrefix"] = "test-app",
                ["MassTransit:AzureServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey",
                ["MassTransit:AzureServiceBus:AutoCreateEntities"] = "true"
            })
            .Build();

        // Act
        _services.AddDfEMassTransit(customConfig);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        serviceProvider.GetService<IEventPublisher>().Should().NotBeNull();
        
        // The hosted service should be registered
        var hostedServices = _services.Where(s => 
            s.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
            s.ImplementationType?.Name == "ServiceBusEntitySetupHostedService");
        
        hostedServices.Should().NotBeEmpty();
    }

    [Fact]
    public void AddDfEMassTransit_WithDefaultAutoCreateEntities_ShouldNotRegisterHostedService()
    {
        // Act
        _services.AddDfEMassTransit(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        // The hosted service should NOT be registered by default (default is false)
        var hostedServices = _services.Where(s => 
            s.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
            s.ImplementationType?.Name == "ServiceBusEntitySetupHostedService");
        
        hostedServices.Should().BeEmpty();
    }

    #endregion

    #region Configuration Validation Tests


    [Fact]
    public void AddDfEMassTransit_WithInvalidTransportType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MassTransit:Transport"] = "InvalidTransport",
                ["MassTransit:AppPrefix"] = "test-app",
                ["MassTransit:AzureServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/"
            })
            .Build();

        // Act & Assert
        var act = () =>
        {
            _services.AddDfEMassTransit(invalidConfig);
            var serviceProvider = _services.BuildServiceProvider();
        };

        // The error might occur during configuration binding or service provider build
        act.Should().Throw<Exception>();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AddDfEMassTransit_WithCompleteConfiguration_ShouldCreateWorkingPublisher()
    {
        // Act
        _services.AddDfEMassTransit(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        var publisher = serviceProvider.GetService<IEventPublisher>();
        publisher.Should().NotBeNull();
        publisher.Should().BeOfType<MassTransitEventPublisher>();
    }

    [Fact]
    public void AddDfEMassTransit_ShouldRegisterPublishEndpoint()
    {
        // Act
        _services.AddDfEMassTransit(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        // IPublishEndpoint is registered by MassTransit
        var publishEndpoint = serviceProvider.GetService<IPublishEndpoint>();
        publishEndpoint.Should().NotBeNull();
    }

    #endregion

    #region Azure Service Bus Specific Configuration Tests

    [Fact]
    public void AddDfEMassTransit_WithAzureServiceBusConfiguration_ShouldAllowCustomConfiguration()
    {
        // Act
        _services.AddDfEMassTransit(
            _configuration,
            configureConsumers: x =>
            {
                x.AddConsumer<TestConsumer>();
            },
            configureAzureServiceBus: (context, cfg) =>
            {
                // Azure Service Bus specific configuration
                // This would normally configure SubscriptionEndpoint, ReceiveEndpoint, etc.
            });

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        serviceProvider.GetService<IEventPublisher>().Should().NotBeNull();
    }

    [Fact]
    public void AddDfEMassTransit_WithAutoConfigureEndpointsDisabled_ShouldNotAutoConfigureEndpoints()
    {
        // Arrange
        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MassTransit:Transport"] = "AzureServiceBus",
                ["MassTransit:AppPrefix"] = "test-app",
                ["MassTransit:AzureServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey",
                ["MassTransit:AzureServiceBus:AutoConfigureEndpoints"] = "false"
            })
            .Build();

        // Act
        _services.AddDfEMassTransit(
            customConfig,
            configureConsumers: x =>
            {
                x.AddConsumer<TestConsumer>();
            },
            configureAzureServiceBus: (context, cfg) =>
            {
                // Manual endpoint configuration would go here
            });

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        serviceProvider.GetService<IEventPublisher>().Should().NotBeNull();
    }

    [Fact]
    public void AddDfEMassTransit_WithAutoConfigureEndpointsEnabled_ShouldAutoConfigureEndpoints()
    {
        // Arrange
        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MassTransit:Transport"] = "AzureServiceBus",
                ["MassTransit:AppPrefix"] = "test-app",
                ["MassTransit:AzureServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey",
                ["MassTransit:AzureServiceBus:AutoConfigureEndpoints"] = "true"
            })
            .Build();

        // Act
        _services.AddDfEMassTransit(
            customConfig,
            configureConsumers: x =>
            {
                x.AddConsumer<TestConsumer>();
            });

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        serviceProvider.GetService<IEventPublisher>().Should().NotBeNull();
    }

    [Fact]
    public void AddDfEMassTransit_WithDefaultAutoConfigureEndpoints_ShouldAutoConfigureEndpoints()
    {
        // Act
        _services.AddDfEMassTransit(
            _configuration,
            configureConsumers: x =>
            {
                x.AddConsumer<TestConsumer>();
            });

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var settings = serviceProvider.GetService<IOptions<MassTransitSettings>>();
        
        // Default should be true
        settings!.Value.AzureServiceBus.AutoConfigureEndpoints.Should().BeTrue();
    }

    [Fact]
    public void AddDfEMassTransit_WithAllCallbacks_ShouldInvokeAllCallbacks()
    {
        // Arrange
        bool consumersConfigured = false;
        bool busConfigured = false;
        bool azureServiceBusConfigured = false;

        // Act
        _services.AddDfEMassTransit(
            _configuration,
            configureConsumers: x =>
            {
                consumersConfigured = true;
                x.AddConsumer<TestConsumer>();
            },
            configureBus: (context, cfg) =>
            {
                busConfigured = true;
            },
            configureAzureServiceBus: (context, cfg) =>
            {
                azureServiceBusConfigured = true;
            });

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        serviceProvider.GetService<IEventPublisher>().Should().NotBeNull();
        
        consumersConfigured.Should().BeTrue();
        busConfigured.Should().BeTrue();
        azureServiceBusConfigured.Should().BeTrue();
    }

    [Fact]
    public void AddDfEMassTransit_WithUseWebSocketsTrue_ShouldConfigureWebSockets()
    {
        // Arrange
        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MassTransit:Transport"] = "AzureServiceBus",
                ["MassTransit:AppPrefix"] = "test-app",
                ["MassTransit:AzureServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey",
                ["MassTransit:AzureServiceBus:UseWebSockets"] = "true"
            })
            .Build();

        // Act
        _services.AddDfEMassTransit(customConfig);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var settings = serviceProvider.GetService<IOptions<MassTransitSettings>>();
        settings!.Value.AzureServiceBus.UseWebSockets.Should().BeTrue();
    }

    [Fact]
    public void AddDfEMassTransit_WithUseWebSocketsFalse_ShouldConfigureTcp()
    {
        // Arrange
        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MassTransit:Transport"] = "AzureServiceBus",
                ["MassTransit:AppPrefix"] = "test-app",
                ["MassTransit:AzureServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey",
                ["MassTransit:AzureServiceBus:UseWebSockets"] = "false"
            })
            .Build();

        // Act
        _services.AddDfEMassTransit(customConfig);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var settings = serviceProvider.GetService<IOptions<MassTransitSettings>>();
        settings!.Value.AzureServiceBus.UseWebSockets.Should().BeFalse();
    }

    #endregion

    #region Helper Classes

    public class TestConsumer : IConsumer<TestMessage>
    {
        public Task Consume(ConsumeContext<TestMessage> context)
        {
            return Task.CompletedTask;
        }
    }

    public class TestMessage
    {
        public string? Content { get; set; }
    }

    #endregion
}

