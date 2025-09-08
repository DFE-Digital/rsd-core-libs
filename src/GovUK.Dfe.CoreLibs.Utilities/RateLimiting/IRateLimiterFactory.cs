namespace GovUK.Dfe.CoreLibs.Utilities.RateLimiting
{
    /// <summary>
    /// Factory for creating configured rate limiters sharing a common state store.
    /// </summary>
    public interface IRateLimiterFactory<in TKey>
        where TKey : notnull
    {
        /// <summary>
        /// Creates a rate limiter allowing <paramref name="maxRequests"/> in <paramref name="interval"/>.
        /// </summary>
        IRateLimiter<TKey> Create(int maxRequests, TimeSpan interval);
    }
}
