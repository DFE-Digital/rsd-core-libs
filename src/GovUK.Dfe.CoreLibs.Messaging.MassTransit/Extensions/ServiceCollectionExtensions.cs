using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Configuration;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Enums;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Helpers;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Interfaces;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Publishers;
using MassTransit;
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
                            cfg.Host(settings.AzureServiceBus.ConnectionString);

                            // Disable automatic topology deployment (entity creation) if AutoCreateEntities is false
                            if (!settings.AzureServiceBus.AutoCreateEntities)
                            {
                                cfg.DeployTopologyOnly = false;
                            }

                            configureBus?.Invoke(context, cfg);

                            cfg.ConfigureEndpoints(context);
                        });
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported transport type: {settings.Transport}");
                }
            });

            // Only register the entity setup hosted service if auto-creation is enabled
            if (settings.Transport == TransportType.AzureServiceBus && settings.AzureServiceBus.AutoCreateEntities)
            {
                services.AddHostedService<ServiceBusEntitySetupHostedService>();
            }

            services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

            return services;
        }
    }
}