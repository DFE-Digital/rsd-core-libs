using System.Runtime.ExceptionServices;

namespace GovUK.Dfe.CoreLibs.AiAgents.Resilience;

/// <summary>
/// Runs work items in parallel and, as soon as one throws, cancels the rest - so an orchestration that
/// has already failed stops spending tokens on agents whose results will be thrown away.
/// </summary>
internal static class FailFastParallel
{
    /// <summary>
    /// Runs every item with a shared token that is cancelled when any item throws, then rethrows that
    /// first failure (not the cancellations it caused in the others).
    /// </summary>
    public static async Task<T[]> WhenAllAsync<T>(IEnumerable<Func<CancellationToken, Task<T>>> work, CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Exception? firstFailure = null;

        var tasks = work.Select(async run =>
        {
            try
            {
                return await run(linked.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Interlocked.CompareExchange(ref firstFailure, ex, null) is null)
                {
                    await linked.CancelAsync().ConfigureAwait(false);
                }

                throw;
            }
        }).ToList();

        try
        {
            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch when (firstFailure is not null && !cancellationToken.IsCancellationRequested)
        {
            // Task.WhenAll surfaces the first failure in list order, which may be a sibling's
            // cancellation; the caller needs the failure that actually stopped the orchestration.
            ExceptionDispatchInfo.Capture(firstFailure).Throw();
            throw;
        }
    }
}
