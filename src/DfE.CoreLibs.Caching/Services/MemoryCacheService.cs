using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Caching.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DfE.CoreLibs.Caching.Services
{
    public class MemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<MemoryCacheService> logger,
        IOptions<CacheSettings> cacheSettings)
        : ICacheService<IMemoryCacheType>
    {
        private readonly MemoryCacheSettings _cacheSettings = cacheSettings.Value.Memory;
        public Type CacheType => typeof(IMemoryCacheType);

        public async Task<T> GetOrAddAsync<T>(string cacheKey, Func<Task<T>> fetchFunction, string methodName)
        {
            if (memoryCache.TryGetValue(cacheKey, out T? cachedValue))
            {
                logger.LogInformation("Cache hit for key: {CacheKey}", cacheKey);
                return cachedValue!;
            }

            logger.LogInformation("Cache miss for key: {CacheKey}. Fetching from source...", cacheKey);
            var result = await fetchFunction();

            if (Equals(result, default(T))) return result;
            var cacheDuration = GetCacheDurationForMethod(methodName);
            memoryCache.Set(cacheKey, result, cacheDuration);
            logger.LogInformation("Cached result for key: {CacheKey} for duration: {CacheDuration}", cacheKey, cacheDuration);

            return result;
        }

        public async Task<T?> GetAsync<T>(string cacheKey)
        {
            await Task.CompletedTask; // Keep async signature for interface consistency
            
            if (memoryCache.TryGetValue(cacheKey, out T? cachedValue))
            {
                logger.LogInformation("Cache hit for key: {CacheKey}", cacheKey);
                return cachedValue;
            }

            logger.LogInformation("Cache miss for key: {CacheKey}", cacheKey);
            return default(T);
        }

        public void Remove(string cacheKey)
        {
            memoryCache.Remove(cacheKey);
            logger.LogInformation("Cache removed for key: {CacheKey}", cacheKey);
        }

        private TimeSpan GetCacheDurationForMethod(string methodName)
        {
            if (_cacheSettings.Durations.TryGetValue(methodName, out int durationInSeconds))
            {
                return TimeSpan.FromSeconds(durationInSeconds);
            }
            return TimeSpan.FromSeconds(_cacheSettings.DefaultDurationInSeconds);
        }
    }
}
