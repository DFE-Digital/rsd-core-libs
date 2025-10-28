using MassTransit;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Consumers
{
    public abstract class BaseConsumer<TMessage>(ILogger logger) : IConsumer<TMessage>
        where TMessage : class
    {
        public async Task Consume(ConsumeContext<TMessage> context)
        {
            try
            {
                await HandleMessageAsync(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message of type {MessageType}", typeof(TMessage).Name);
                throw;
            }
        }

        protected abstract Task HandleMessageAsync(ConsumeContext<TMessage> context);
    }
}
