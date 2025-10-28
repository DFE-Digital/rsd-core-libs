using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Configuration;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Helpers
{
    [ExcludeFromCodeCoverage] // Depends on ServiceBusAdminHelper which requires real Azure Service Bus connection
    public class ServiceBusEntitySetupHostedService(IOptions<MassTransitSettings> options, ILogger<ServiceBusEntitySetupHostedService> logger) : IHostedService
    {
        private readonly MassTransitSettings _settings = options.Value;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_settings.Transport == TransportType.AzureServiceBus)
            {
                logger.LogInformation("Starting Service Bus entity setup...");

                await ServiceBusAdminHelper.EnsureEntitiesExistAsync(_settings.AzureServiceBus.ConnectionString, logger);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
