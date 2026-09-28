using GovUK.Dfe.CoreLibs.AiAgents.Resilience;
using System.Diagnostics;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Resilience;

public sealed class FailFastParallelTests
{
    [Fact]
    public async Task AFailure_CancelsTheOthers_AndIsTheExceptionThrown()
    {
        var failure = new InvalidOperationException("Mandatory agent failed.");
        var siblingCancelled = false;
        var stopwatch = Stopwatch.StartNew();

        var work = new Func<CancellationToken, Task<int>>[]
        {
            // Listed first, so plain Task.WhenAll would surface its cancellation instead of the real failure.
            async token =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), token);
                    return 1;
                }
                catch (OperationCanceledException)
                {
                    siblingCancelled = true;
                    throw;
                }
            },
            async ct =>
            {
                await Task.Delay(20, ct);
                throw failure;
            },
        };

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => FailFastParallel.WhenAllAsync(work, CancellationToken.None));

        Assert.Same(failure, thrown);
        Assert.True(siblingCancelled);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), "The slow sibling should have been cancelled, not waited for.");
    }

    [Fact]
    public async Task WithNoFailures_ReturnsEveryResult_InOrder()
    {
        var results = await FailFastParallel.WhenAllAsync(
            [async ct => { await Task.Delay(30, ct); return 1; }, _ => Task.FromResult(2)], CancellationToken.None);

        Assert.Equal([1, 2], results);
    }

    [Fact]
    public async Task CallerCancellation_Propagates()
    {
        using var caller = new CancellationTokenSource();
        await caller.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FailFastParallel.WhenAllAsync(
            [token => Task.Delay(TimeSpan.FromMinutes(5), token).ContinueWith(_ => 1, token)], caller.Token));
    }
}
