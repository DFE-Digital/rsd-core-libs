using System.Text.Json;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Caching.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GovUK.Dfe.CoreLibs.Caching.Services
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

        private const int LockExpirySeconds = 30;
        private const int LockRetryDelayMilliseconds = 50;
        private const int MaxLockRetries = 100;

        public Type CacheType => typeof(IRedisCacheType);

        public async Task<T> GetOrAddAsync<T>(string cacheKey, Func<Task<T>> fetchFunction, string methodName, CancellationToken cancellationToken = default)
        {
            var fullKey = GetFullKey(cacheKey);

            try
            {
                // First check: try to get from cache without lock
                var cachedValue = await _database.StringGetAsync(fullKey);
                if (cachedValue.HasValue)
                {
                    logger.LogInformation("Redis cache hit for key: {CacheKey}", fullKey);
                    var deserializedValue = JsonSerializer.Deserialize<T>(cachedValue!, _jsonSerializerOptions);
                    return deserializedValue!;
                }

                // Cache miss - acquire distributed lock to prevent race conditions
                var lockKey = $"{fullKey}:lock";
                var lockToken = Guid.NewGuid().ToString();
                
                var lockAcquired = await TryAcquireLockAsync(lockKey, lockToken, cancellationToken);
                
                if (lockAcquired)
                {
                    try
                    {
                        // Double-check: another thread might have populated the cache while we were waiting for the lock
                        cachedValue = await _database.StringGetAsync(fullKey);
                        if (cachedValue.HasValue)
                        {
                            logger.LogInformation("Redis cache hit after lock acquisition for key: {CacheKey}", fullKey);
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
                    finally
                    {
                        // Release the lock
                        await ReleaseLockAsync(lockKey, lockToken, cancellationToken);
                    }
                }
                else
                {
                    // Could not acquire lock - wait and retry reading from cache
                    logger.LogInformation("Waiting for another thread to populate cache for key: {CacheKey}", fullKey);
                    
                    for (int i = 0; i < MaxLockRetries; i++)
                    {
                        await Task.Delay(LockRetryDelayMilliseconds, cancellationToken);
                        
                        cachedValue = await _database.StringGetAsync(fullKey);
                        if (cachedValue.HasValue)
                        {
                            logger.LogInformation("Redis cache hit after waiting for key: {CacheKey}", fullKey);
                            var deserializedValue = JsonSerializer.Deserialize<T>(cachedValue!, _jsonSerializerOptions);
                            return deserializedValue!;
                        }
                    }
                    
                    // Fallback: if we still don't have the value after max retries, fetch it ourselves
                    logger.LogWarning("Max retries reached waiting for cache population for key: {CacheKey}. Fetching directly.", fullKey);
                    return await fetchFunction();
                }
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

        public async Task<T?> GetAsync<T>(string cacheKey)
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

        /// <summary>
        /// Attempts to acquire a distributed lock using Redis SETNX command.
        /// </summary>
        /// <param name="lockKey">The key to use for the lock.</param>
        /// <param name="lockToken">A unique token to identify this lock owner.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>True if the lock was acquired, false otherwise.</returns>
        private async Task<bool> TryAcquireLockAsync(string lockKey, string lockToken, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Use SET with NX (only set if not exists) and EX (expiry) options
                var acquired = await _database.StringSetAsync(
                    lockKey,
                    lockToken,
                    TimeSpan.FromSeconds(LockExpirySeconds),
                    When.NotExists);

                if (acquired)
                {
                    logger.LogDebug("Lock acquired for key: {LockKey}", lockKey);
                }
                else
                {
                    logger.LogDebug("Failed to acquire lock for key: {LockKey}", lockKey);
                }

                return acquired;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Lock acquisition cancelled for key: {LockKey}", lockKey);
                throw;
            }
            catch (RedisException ex)
            {
                logger.LogError(ex, "Error acquiring lock for key: {LockKey}", lockKey);
                return false;
            }
        }

        /// <summary>
        /// Releases a distributed lock using a Lua script to ensure only the lock owner can release it.
        /// </summary>
        /// <param name="lockKey">The key used for the lock.</param>
        /// <param name="lockToken">The unique token that was used to acquire the lock.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        private async Task ReleaseLockAsync(string lockKey, string lockToken, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Lua script to atomically check and delete the lock only if the token matches
                const string luaScript = @"
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('del', KEYS[1])
                    else
                        return 0
                    end";

                var result = await _database.ScriptEvaluateAsync(
                    luaScript,
                    new RedisKey[] { lockKey },
                    new RedisValue[] { lockToken });

                if ((int)result == 1)
                {
                    logger.LogDebug("Lock released for key: {LockKey}", lockKey);
                }
                else
                {
                    logger.LogWarning("Failed to release lock for key: {LockKey}. Lock may have expired or been acquired by another process.", lockKey);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Lock release cancelled for key: {LockKey}", lockKey);
                // Don't rethrow here - we're in cleanup, best effort to release the lock
            }
            catch (RedisException ex)
            {
                logger.LogError(ex, "Error releasing lock for key: {LockKey}", lockKey);
            }
        }
    }
}