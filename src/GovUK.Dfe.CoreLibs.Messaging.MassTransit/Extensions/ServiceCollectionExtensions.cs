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
        public static IServiceCollection AddDfEMassTransit(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<IBusRegistrationConfigurator>? configureConsumers = null,
            Action<IBusRegistrationContext, IBusFactoryConfigurator>? configureBus = null
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
                        x.UsingAzureServiceBus((context, cfg) =>
                        {
                            cfg.Host(settings.AzureServiceBus.ConnectionString, hostCfg =>
                            {
                                hostCfg.TransportType = settings.AzureServiceBus.UseWebSockets
                                    ? ServiceBusTransportType.AmqpWebSockets
                                    : ServiceBusTransportType.AmqpTcp;
                            });

                            cfg.DeployPublishTopology = settings.AzureServiceBus.AutoCreateEntities;

                            configureBus?.Invoke(context, cfg);

                            cfg.ConfigureEndpoints(context);
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