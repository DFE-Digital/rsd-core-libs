using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Configuration;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Enums;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Helpers;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Interfaces;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Publishers;
using MassTransit;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private static string GetServiceBusConnectionString(IConfiguration configuration, MassTransitSettings settings)
        {
            // First, check ConnectionStrings:ServiceBus
            var connectionString = configuration.GetConnectionString("ServiceBus") ??
                                   configuration["ServiceBus"];

            if (!string.IsNullOrEmpty(connectionString))
            {
                return connectionString;
            }

            // Fall back to MassTransit:AzureServiceBus:ConnectionString
            if (!string.IsNullOrEmpty(settings.AzureServiceBus.ConnectionString))
            {
                return settings.AzureServiceBus.ConnectionString;
            }

            throw new InvalidOperationException(
                "Azure Service Bus connection string is required but not configured. " +
                "Please configure either 'ConnectionStrings:ServiceBus' or 'MassTransit:AzureServiceBus:ConnectionString'");
        }

        /// <summary>
        /// Adds DfE MassTransit configuration with support for advanced Azure Service Bus features.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="configureConsumers">Optional: Configure consumers and sagas</param>
        /// <param name="configureBus">Optional: Configure bus settings (for generic transport configuration)</param>
        /// <param name="configureAzureServiceBus">Optional: Configure Azure Service Bus specific features (SubscriptionEndpoint, ReceiveEndpoint, Subscribe, SQL filters, etc.)</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddDfEMassTransit(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<IBusRegistrationConfigurator>? configureConsumers = null,
            Action<IBusRegistrationContext, IBusFactoryConfigurator>? configureBus = null,
            Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator>? configureAzureServiceBus = null
        )
        {
            services.AddOptions<MassTransitSettings>()
                .Bind(configuration.GetSection("MassTransit"))
                .Configure(options => { });

            var settings = configuration
                .GetSection("MassTransit")
                .Get<MassTransitSettings>()
                ?? throw new InvalidOperationException("MassTransit configuration section is missing or invalid.");

            services.AddMassTransit(x =>
            {
                configureConsumers?.Invoke(x);

                x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(settings.AppPrefix, false));

                switch (settings.Transport)
                {
                    case TransportType.AzureServiceBus:
                        var connectionString = GetServiceBusConnectionString(configuration, settings);
                        
                        x.UsingAzureServiceBus((context, cfg) =>
                        {
                            cfg.Host(connectionString, hostCfg =>
                            {
                                hostCfg.TransportType = settings.AzureServiceBus.UseWebSockets
                                    ? ServiceBusTransportType.AmqpWebSockets
                                    : ServiceBusTransportType.AmqpTcp;
                            });

                            cfg.DeployPublishTopology = settings.AzureServiceBus.AutoCreateEntities;

                            // Generic bus configuration (applies to all transports)
                            configureBus?.Invoke(context, cfg);

                            // Azure Service Bus specific configuration
                            // This is where users can access SubscriptionEndpoint, ReceiveEndpoint, Subscribe, SQL filters, etc.
                            configureAzureServiceBus?.Invoke(context, cfg);

                            // Only configure endpoints automatically if no custom Azure Service Bus configuration is provided
                            // This gives users full control over endpoint configuration
                            if (configureAzureServiceBus == null && settings.AzureServiceBus.AutoConfigureEndpoints)
                            {
                                cfg.ConfigureEndpoints(context);
                            }
                        });
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported transport type: {settings.Transport}");
                }
            });

            if (settings.Transport == TransportType.AzureServiceBus && settings.AzureServiceBus.AutoCreateEntities)
            {
                services.AddHostedService<ServiceBusEntitySetupHostedService>();
            }

            services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

            return services;
        }
    }
}