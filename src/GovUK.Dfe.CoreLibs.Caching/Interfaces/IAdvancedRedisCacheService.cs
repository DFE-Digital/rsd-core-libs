namespace GovUK.Dfe.CoreLibs.Caching.Interfaces
{
    /// <summary>
    /// Advanced Redis cache operations including pattern matching and raw data.
    /// For most use cases, use ICacheService&lt;IRedisCacheType&gt; instead.
    /// This interface extends the standard cache service with advanced operations needed for
    /// session management and binary data storage.
    /// </summary>
    public interface IAdvancedRedisCacheService : ICacheService<IRedisCacheType>
    {
        /// <summary>
        /// Sets raw byte data directly without JSON serialization (for session support and binary data)
        /// </summary>
        /// <param name="cacheKey">The cache key</param>
        /// <param name="value">The raw byte array to store</param>
        /// <param name="expiry">How long to cache the data</param>
        Task SetRawAsync(string cacheKey, byte[] value, TimeSpan expiry);

        /// <summary>
        /// Gets raw byte data directly without JSON deserialization (for session support and binary data)
        /// </summary>
        /// <param name="cacheKey">The cache key</param>
        /// <returns>The raw byte array, or null if not found</returns>
        Task<byte[]?> GetRawAsync(string cacheKey);

        /// <summary>
        /// Removes a cache entry asynchronously
        /// </summary>
        /// <param name="cacheKey">The cache key to remove</param>
        Task RemoveAsync(string cacheKey);

        /// <summary>
        /// Removes all cache entries matching a Redis pattern (e.g., "user:123:*")
        /// Use with caution - this scans all keys in the database
        /// </summary>
        /// <param name="pattern">Redis pattern with wildcards (* for any characters)</param>
        Task RemoveByPatternAsync(string pattern);
    }
}