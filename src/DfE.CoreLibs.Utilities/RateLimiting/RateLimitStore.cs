using System.Collections.Concurrent;

namespace DfE.CoreLibs.Utilities.RateLimiting
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
