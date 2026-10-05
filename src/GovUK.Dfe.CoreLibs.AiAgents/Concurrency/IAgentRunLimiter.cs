namespace GovUK.Dfe.CoreLibs.AiAgents.Concurrency;

/// <summary>Limits how many agent runs happen at once. Every Foundry run holds a slot while it runs.</summary>
public interface IAgentRunLimiter
{
    /// <summary>Waits for a free slot. Dispose the result to free it.</summary>
    /// <exception cref="TimeoutException">No slot came free within <see cref="AgentRunOptions.MaxWaitForRunSlot"/>.</exception>
    ValueTask<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken = default);
}
