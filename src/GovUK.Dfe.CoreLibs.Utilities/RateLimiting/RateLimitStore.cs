using System.Collections.Concurrent;

namespace GovUK.Dfe.CoreLibs.Utilities.RateLimiting
{
    /// <summary>
    /// In-memory store of request timestamps per key, shared across limiters.
    /// </summary>
    public class RateLimitStore<TKey>
        where TKey : notnull
    {
        public ConcurrentDictionary<TKey, Queue<DateTime>> Logs { get; } = new();
    }
}
