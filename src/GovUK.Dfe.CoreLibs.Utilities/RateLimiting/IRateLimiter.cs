namespace GovUK.Dfe.CoreLibs.Utilities.RateLimiting
{
    /// <summary>
    /// Contract for a rate limiter that determines if a request is allowed for a given key.
    /// </summary>
    public interface IRateLimiter<in TKey>
        where TKey : notnull
    {
        bool IsAllowed(TKey key);
    }
}
