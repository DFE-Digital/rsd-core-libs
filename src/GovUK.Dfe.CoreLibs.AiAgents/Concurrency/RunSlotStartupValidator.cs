using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.CoreLibs.AiAgents.Concurrency;

/// <summary>
/// With <c>GlobalConcurrency</c> set, checks at startup that the run slot container exists (creating it if not) and
/// that this app's identity can use it. Missing permission fails startup; an unreachable account only warns.
/// </summary>
internal sealed class RunSlotStartupValidator(BlobRunSlotStore store, ILogger<RunSlotStartupValidator> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await store.EnsureAccessAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not OperationCanceledException)
        {
            logger.LogWarning(ex, "Couldn't check the run slot container {Container} at startup; runs will retry it", store.ContainerUri);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
