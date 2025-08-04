namespace DfE.CoreLibs.Utilities.RateLimiting
{
    /// <summary>
    /// A time-window based rate limiter using a shared store.
    /// </summary>
    public class TimeBasedRateLimiter<TKey> : IRateLimiter<TKey>
        where TKey : notnull
    {
        private readonly int _maxRequests;
        private readonly TimeSpan _interval;
        private readonly RateLimitStore<TKey> _store;
        private readonly Func<DateTime> _timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeBasedRateLimiter{TKey}"/> class.
        /// </summary>
        public TimeBasedRateLimiter(
            int maxRequests,
            TimeSpan interval,
            RateLimitStore<TKey> store,
            Func<DateTime> timeProvider)
        {
            if (maxRequests <= 0) throw new ArgumentOutOfRangeException(nameof(maxRequests));
            if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
            _maxRequests = maxRequests;
            _interval = interval;
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _timeProvider = timeProvider;
        }

        /// <summary>
        /// Determines if a request for the specified key is allowed.
        /// </summary>
        public bool IsAllowed(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var now = _timeProvider();
            var history = _store.Logs.GetOrAdd(key, _ => new Queue<DateTime>());
            lock (history)
            {
                while (history.Count > 0 && now - history.Peek() >= _interval)
                    history.Dequeue();

                if (history.Count < _maxRequests)
                {
                    history.Enqueue(now);
                    return true;
                }
                return false;
            }
        }
    }
}