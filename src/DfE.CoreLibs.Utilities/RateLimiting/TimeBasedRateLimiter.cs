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
        private readonly Queue<DateTime> _history;
        private readonly Func<DateTime> _timeProvider;

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
            _history = store.Logs.GetOrAdd(default!, _ => new Queue<DateTime>());
            _timeProvider = timeProvider;
        }

        public bool IsAllowed(TKey key)
        {
            var now = _timeProvider();
            lock (_history)
            {
                while (_history.Count > 0 && now - _history.Peek() >= _interval)
                    _history.Dequeue();

                if (_history.Count < _maxRequests)
                {
                    _history.Enqueue(now);
                    return true;
                }
                return false;
            }
        }
    }
}