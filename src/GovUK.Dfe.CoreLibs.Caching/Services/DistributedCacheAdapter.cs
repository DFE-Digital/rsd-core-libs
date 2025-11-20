using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.CoreLibs.Caching.Services
{
    /// <summary>
    /// Adapter that enables IAdvancedRedisCacheService to work as IDistributedCache
    /// for ASP.NET Core session support. This is registered automatically when using
    /// AddHybridCaching() and enables Redis-backed sessions with zero configuration.
    /// </summary>
    public class DistributedCacheAdapter(
        IAdvancedRedisCacheService cacheService,
        ILogger<DistributedCacheAdapter> logger)
        : IDistributedCache
    {
        public byte[]? Get(string key)
        {
            return GetAsync(key).GetAwaiter().GetResult();
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            try
            {
                return await cacheService.GetRawAsync(key);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting distributed cache value for key: {Key}", key);
                return null;
            }
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            SetAsync(key, value, options).GetAwaiter().GetResult();
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            try
            {
                var expiry = options.AbsoluteExpirationRelativeToNow
                    ?? options.SlidingExpiration
                    ?? TimeSpan.FromMinutes(20);

                await cacheService.SetRawAsync(key, value, expiry);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting distributed cache value for key: {Key}", key);
                throw;
            }
        }

        public void Refresh(string key)
        {
            RefreshAsync(key).GetAwaiter().GetResult();
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            try
            {
                // Refresh extends the expiration by re-setting the value
                var value = await GetAsync(key, token);
                if (value != null)
                {
                    await SetAsync(key, value, new DistributedCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(20)
                    }, token);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing distributed cache key: {Key}", key);
            }
        }

        public void Remove(string key)
        {
            RemoveAsync(key).GetAwaiter().GetResult();
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            try
            {
                await cacheService.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error removing distributed cache key: {Key}", key);
            }
        }
    }
}