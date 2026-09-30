using GovUK.Dfe.CoreLibs.AiAgents.Concurrency;
using GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration.Fakes;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Concurrency;

public sealed class AgentRunLimiterTests
{
    private readonly CancellationToken cancellationToken = default;

    private static AgentRunLimiter Instance(IRunSlotStore? global = null, int? maxConcurrency = null, TimeSpan? maxWait = null)
        => new(new AgentRunOptions { MaxConcurrency = maxConcurrency, MaxWaitForRunSlot = maxWait ?? TimeSpan.FromSeconds(30) }, global);

    [Fact]
    public async Task WaitingLongerThanMaxWait_FailsWithTimeout_AndGivesBackTheInstanceSlot()
    {
        var store = new InMemoryRunSlotStore(capacity: 1);
        using var busy = Instance(store);
        using var limiter = Instance(store, maxConcurrency: 1, maxWait: TimeSpan.FromMilliseconds(300));
        var elsewhere = await busy.AcquireAsync(cancellationToken);

        await Assert.ThrowsAsync<TimeoutException>(() => limiter.AcquireAsync(cancellationToken).AsTask());

        await elsewhere.DisposeAsync();
        await using var slot = await limiter.AcquireAsync(cancellationToken);   // the instance slot wasn't leaked
    }

    [Fact]
    public async Task CallerCancellation_IsReportedAsCancellation_NotATimeout()
    {
        using var limiter = Instance(maxConcurrency: 1);
        await using var held = await limiter.AcquireAsync(cancellationToken);
        using var cancel = new CancellationTokenSource(100);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => limiter.AcquireAsync(cancel.Token).AsTask());
    }
}
