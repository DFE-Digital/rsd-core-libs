namespace GovUK.Dfe.CoreLibs.AiAgents.Concurrency;

/// <summary>
/// Run slots shared by every instance, for the global limit. The built-in store uses blob leases; register
/// another (e.g. Redis) with <c>agents.Services.AddSingleton&lt;IRunSlotStore&gt;(...)</c>.
/// </summary>
public interface IRunSlotStore
{
    /// <summary>How many runs may hold a slot at once, across all instances.</summary>
    int Capacity { get; }

    /// <summary>
    /// Takes slot <paramref name="slot"/> (0 to <see cref="Capacity"/> - 1) if it's free. The slot must stay
    /// held until the result is disposed, and free itself soon after if this instance dies.
    /// </summary>
    /// <returns>The held slot, or <see langword="null"/> if another run holds it.</returns>
    Task<IAsyncDisposable?> TryAcquireAsync(int slot, CancellationToken cancellationToken = default);
}
