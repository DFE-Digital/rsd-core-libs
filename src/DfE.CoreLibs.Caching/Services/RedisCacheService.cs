using System.Text.Json;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Caching.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DfE.CoreLibs.Caching.Services
{
    public class RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheService> logger,
        IOptions<CacheSettings> cacheSettings)
        : ICacheService<IRedisCacheType>
    {
        private readonly RedisCacheSettings _cacheSettings = cacheSettings.Value.Redis;
        private readonly IDatabase _database = connectionMultiplexer.GetDatabase(cacheSettings.Value.Redis.Database);
        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public Type CacheType => typeof(IRedisCacheType);

        public async Task<T> GetOrAddAsync<T>(string cacheKey, Func<Task<T>> fetchFunction, string methodName)
        {
            var fullKey = GetFullKey(cacheKey);

            try
            {
                var cachedValue = await _database.StringGetAsync(fullKey);
                if (cachedValue.HasValue)
                {
                    logger.LogInformation("Redis cache hit for key: {CacheKey}", fullKey);
                    var deserializedValue = JsonSerializer.Deserialize<T>(cachedValue!, _jsonSerializerOptions);
                    return deserializedValue!;
                }

                logger.LogInformation("Redis cache miss for key: {CacheKey}. Fetching from source...", fullKey);
                var result = await fetchFunction();

                if (!Equals(result, default(T)))
                {
                    var cacheDuration = GetCacheDurationForMethod(methodName);
                    var serializedValue = JsonSerializer.Serialize(result, _jsonSerializerOptions);
                    await _database.StringSetAsync(fullKey, serializedValue, cacheDuration);
                    logger.LogInformation("Redis cached result for key: {CacheKey} for duration: {CacheDuration}", fullKey, cacheDuration);
                }

                return result;
            }
            catch (RedisException ex)
            {
                logger.LogError(ex, "Redis error occurred for key: {CacheKey}. Falling back to fetch function", fullKey);
                return await fetchFunction();
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "JSON serialization error for key: {CacheKey}. Removing invalid cache entry", fullKey);
                await _database.KeyDeleteAsync(fullKey);
                return await fetchFunction();
            }
        }

        public async Task<T?> GetAsync<T>(string cacheKey, string methodName)
        {
            var fullKey = GetFullKey(cacheKey);

            try
            {
                var cachedValue = await _database.StringGetAsync(fullKey);
                if (cachedValue.HasValue)
                {
                    logger.LogInformation("Redis cache hit for key: {CacheKey}", fullKey);
                    var deserializedValue = JsonSerializer.Deserialize<T>(cachedValue!, _jsonSerializerOptions);
                    return deserializedValue;
                }

                logger.LogInformation("Redis cache miss for key: {CacheKey}", fullKey);
                return default(T);
            }
            catch (RedisException ex)
            {
                logger.LogError(ex, "Redis error occurred for key: {CacheKey}. Returning default value", fullKey);
                return default(T);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "JSON deserialization error for key: {CacheKey}. Removing invalid cache entry", fullKey);
                await _database.KeyDeleteAsync(fullKey);
                return default(T);
            }
        }

        public void Remove(string cacheKey)
        {
            var fullKey = GetFullKey(cacheKey);
            try
            {
                _database.KeyDelete(fullKey);
                logger.LogInformation("Redis cache removed for key: {CacheKey}", fullKey);
            }
            catch (RedisException ex)
            {
                logger.LogError(ex, "Redis error occurred while removing key: {CacheKey}", fullKey);
            }
        }

        public async Task RemoveAsync(string cacheKey)
        {
            var fullKey = GetFullKey(cacheKey);
            try
            {
                await _database.KeyDeleteAsync(fullKey);
                logger.LogInformation("Redis cache removed for key: {CacheKey}", fullKey);
            }
            catch (RedisException ex)
            {
                logger.LogError(ex, "Redis error occurred while removing key: {CacheKey}", fullKey);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            var fullPattern = GetFullKey(pattern);
            try
            {
                var server = connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints()[0]);
                var keys = server.Keys(database: _cacheSettings.Database, pattern: fullPattern);
                
                foreach (var key in keys)
                {
                    await _database.KeyDeleteAsync(key);
                }
                
                logger.LogInformation("Redis cache entries removed for pattern: {Pattern}", fullPattern);
            }
            catch (RedisException ex)
            {
                logger.LogError(ex, "Redis error occurred while removing keys by pattern: {Pattern}", fullPattern);
            }
        }

        private string GetFullKey(string key)
        {
            return $"{_cacheSettings.KeyPrefix}{key}";
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
