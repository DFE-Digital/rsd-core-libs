namespace GovUK.Dfe.CoreLibs.Utilities.RateLimiting
{
    /// <summary>
    /// Default factory implementing <see cref="IRateLimiterFactory{TKey}"/> using <see cref="TimeBasedRateLimiter{TKey}"/>.
    /// </summary>
    public class RateLimiterFactory<TKey>(RateLimitStore<TKey> store, Func<DateTime>? timeProvider = null)
        : IRateLimiterFactory<TKey>
        where TKey : notnull
    {
        private readonly Func<DateTime> _timeProvider = timeProvider ?? (() => DateTime.UtcNow);

        public IRateLimiter<TKey> Create(int maxRequests, TimeSpan interval)
            => new TimeBasedRateLimiter<TKey>(maxRequests, interval, store, _timeProvider);
    }
}
