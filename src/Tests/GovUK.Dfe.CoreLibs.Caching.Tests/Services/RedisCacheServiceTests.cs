using System.Net;
using System.Text.Json;
using AutoFixture;
using GovUK.Dfe.CoreLibs.Caching.Services;
using GovUK.Dfe.CoreLibs.Caching.Settings;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GraphQL.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;

namespace GovUK.Dfe.CoreLibs.Caching.Tests.Services
{
    public class RedisCacheServiceTests
    {
        private readonly IFixture _fixture;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _database;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly IOptions<CacheSettings> _options;
        private readonly RedisCacheService _cacheService;
        private readonly RedisCacheSettings _redisCacheSettings;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public RedisCacheServiceTests()
        {
            _fixture = new Fixture();
            _connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
            _database = Substitute.For<IDatabase>();
            _logger = Substitute.For<ILogger<RedisCacheService>>();

            _redisCacheSettings = new RedisCacheSettings
            {
                DefaultDurationInSeconds = 300,
                Durations = new Dictionary<string, int> { { "TestMethod", 600 } },
                KeyPrefix = "Test:",
                Database = 0
            };
            var settings = new CacheSettings { Redis = _redisCacheSettings };
            _options = Options.Create(settings);

            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            _connectionMultiplexer.GetDatabase(_redisCacheSettings.Database).Returns(_database);

            _cacheService = new RedisCacheService(_connectionMultiplexer, _logger, _options);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldReturnCachedValue_WhenCacheKeyExists(string cacheKey, string methodName)
        {
            // Arrange
            var cachedValue = _fixture.Create<string>();
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var serializedValue = JsonSerializer.Serialize(cachedValue);

            _database.StringGetAsync(fullKey).Returns(new RedisValue(serializedValue));

            // Act
            var result = await _cacheService.GetOrAddAsync(cacheKey, () => Task.FromResult(cachedValue), methodName);

            // Assert
            Assert.Equal(cachedValue, result);
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis cache hit for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldFetchAndCacheValue_WhenCacheKeyDoesNotExist(string cacheKey, string methodName)
        {
            // Arrange
            var expectedValue = _fixture.Create<string>();
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var lockKey = $"{fullKey}:lock";

            // First call returns null (cache miss), second call after lock also returns null
            _database.StringGetAsync(fullKey).Returns(RedisValue.Null, RedisValue.Null);
            
            // Lock acquisition succeeds
            _database.StringSetAsync(
                Arg.Is<RedisKey>(k => k.ToString() == lockKey), 
                Arg.Any<RedisValue>(), 
                Arg.Any<TimeSpan>(), 
                When.NotExists)
                .Returns(true);
            
            // Cache set succeeds
            _database.StringSetAsync(fullKey, Arg.Any<RedisValue>(), TimeSpan.FromSeconds(_redisCacheSettings.DefaultDurationInSeconds))
                .Returns(Task.FromResult(true));

            // Mock Lua script execution for lock release
            _database.ScriptEvaluateAsync(
                Arg.Any<string>(),
                Arg.Any<RedisKey[]>(),
                Arg.Any<RedisValue[]>())
                .Returns(RedisResult.Create(1));

            // Act
            var result = await _cacheService.GetOrAddAsync(cacheKey, () => Task.FromResult(expectedValue), methodName);

            // Assert
            Assert.Equal(expectedValue, result);
            await _database.Received(1).StringSetAsync(
                fullKey,
                Arg.Any<RedisValue>(),
                TimeSpan.FromSeconds(_redisCacheSettings.DefaultDurationInSeconds));

            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis cache miss for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);

            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis cached result for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldUseCustomDuration_WhenMethodSpecificDurationExists(string cacheKey)
        {
            // Arrange
            var expectedValue = _fixture.Create<string>();
            var methodName = "TestMethod";
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var lockKey = $"{fullKey}:lock";
            var expectedDuration = TimeSpan.FromSeconds(_redisCacheSettings.Durations[methodName]);

            _database.StringGetAsync(fullKey).Returns(RedisValue.Null, RedisValue.Null);
            
            // Lock acquisition succeeds
            _database.StringSetAsync(
                Arg.Is<RedisKey>(k => k.ToString() == lockKey), 
                Arg.Any<RedisValue>(), 
                Arg.Any<TimeSpan>(), 
                When.NotExists)
                .Returns(true);
            
            _database.StringSetAsync(fullKey, Arg.Any<RedisValue>(), expectedDuration)
                .Returns(Task.FromResult(true));

            // Mock Lua script execution for lock release
            _database.ScriptEvaluateAsync(
                Arg.Any<string>(),
                Arg.Any<RedisKey[]>(),
                Arg.Any<RedisValue[]>())
                .Returns(RedisResult.Create(1));

            // Act
            var result = await _cacheService.GetOrAddAsync(cacheKey, () => Task.FromResult(expectedValue), methodName);

            // Assert
            Assert.Equal(expectedValue, result);
            await _database.Received(1).StringSetAsync(fullKey, Arg.Any<RedisValue>(), expectedDuration);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldFallbackToFetchFunction_WhenRedisExceptionOccurs(string cacheKey, string methodName)
        {
            // Arrange
            var expectedValue = _fixture.Create<string>();
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";

            _database.StringGetAsync(fullKey).Returns(Task.FromException<RedisValue>(new RedisException("Redis connection failed")));

            // Act
            var result = await _cacheService.GetOrAddAsync(cacheKey, () => Task.FromResult(expectedValue), methodName);

            // Assert
            Assert.Equal(expectedValue, result);
            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis error occurred for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldRemoveInvalidCacheAndFetch_WhenJsonExceptionOccurs(string cacheKey, string methodName)
        {
            // Arrange
            var expectedValue = _fixture.Create<string>();
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var invalidJson = "invalid-json-data";

            _database.StringGetAsync(fullKey).Returns(new RedisValue(invalidJson));

            // Act
            var result = await _cacheService.GetOrAddAsync(cacheKey, () => Task.FromResult(expectedValue), methodName);

            // Assert
            Assert.Equal(expectedValue, result);
            await _database.Received(1).KeyDeleteAsync(fullKey);
            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"JSON serialization error for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldNotCache_WhenResultIsDefaultValue(string cacheKey, string methodName)
        {
            // Arrange
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var lockKey = $"{fullKey}:lock";

            _database.StringGetAsync(fullKey).Returns(RedisValue.Null, RedisValue.Null);
            
            // Lock acquisition succeeds
            _database.StringSetAsync(
                Arg.Is<RedisKey>(k => k.ToString() == lockKey), 
                Arg.Any<RedisValue>(), 
                Arg.Any<TimeSpan>(), 
                When.NotExists)
                .Returns(true);

            // Mock Lua script execution for lock release
            _database.ScriptEvaluateAsync(
                Arg.Any<string>(),
                Arg.Any<RedisKey[]>(),
                Arg.Any<RedisValue[]>())
                .Returns(RedisResult.Create(1));

            // Act
            var result = await _cacheService.GetOrAddAsync<string>(cacheKey, () => Task.FromResult<string>(null!), methodName);

            // Assert
            Assert.Null(result);
            // Should only set the lock, not the actual cache value
            await _database.Received(1).StringSetAsync(
                Arg.Is<RedisKey>(k => k.ToString() == lockKey), 
                Arg.Any<RedisValue>(), 
                Arg.Any<TimeSpan>(), 
                When.NotExists);
            await _database.DidNotReceive().StringSetAsync(
                Arg.Is<RedisKey>(k => k.ToString() == fullKey), 
                Arg.Any<RedisValue>(), 
                Arg.Any<TimeSpan>());
        }

        [Theory]
        [CustomAutoData()]
        public void Remove_ShouldRemoveValueFromCache_WhenCalled(string cacheKey)
        {
            // Arrange
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";

            // Act
            _cacheService.Remove(cacheKey);

            // Assert
            _database.Received(1).KeyDelete(fullKey);
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis cache removed for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task RemoveAsync_ShouldRemoveValueFromCache_WhenCalled(string cacheKey)
        {
            // Arrange
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";

            // Act
            await _cacheService.RemoveAsync(cacheKey);

            // Assert
            await _database.Received(1).KeyDeleteAsync(fullKey);
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis cache removed for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public void Remove_ShouldLogError_WhenRedisExceptionOccurs(string cacheKey)
        {
            // Arrange
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            _database.When(x => x.KeyDelete(fullKey)).Do(x => throw new RedisException("Redis connection failed"));

            // Act
            _cacheService.Remove(cacheKey);

            // Assert
            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis error occurred while removing key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task RemoveByPatternAsync_ShouldRemoveMatchingKeys_WhenCalled(string pattern)
        {
            // Arrange
            var fullPattern = $"{_redisCacheSettings.KeyPrefix}{pattern}";
            var keys = new RedisKey[] { "key1", "key2", "key3" };
            var server = Substitute.For<IServer>();
            var endpoints = new EndPoint[] { new DnsEndPoint("localhost", 6379) };

            _connectionMultiplexer.GetEndPoints().Returns(args => endpoints);
            _connectionMultiplexer.GetServer(endpoints[0]).Returns(server);
            server.Keys(database: _redisCacheSettings.Database, pattern: fullPattern).Returns(keys);

            // Act
            await _cacheService.RemoveByPatternAsync(pattern);

            // Assert
            foreach (var key in keys)
            {
                await _database.Received(1).KeyDeleteAsync(key);
            }
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis cache entries removed for pattern: {fullPattern}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetAsync_ShouldReturnCachedValue_WhenCacheKeyExists(string cacheKey, string methodName, string expectedValue)
        {
            // Arrange
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var serializedValue = JsonSerializer.Serialize(expectedValue, _jsonSerializerOptions);
            _database.StringGetAsync(fullKey).Returns(new RedisValue(serializedValue));

            // Act
            var result = await _cacheService.GetAsync<string>(cacheKey);

            // Assert
            Assert.Equal(expectedValue, result);
            await _database.Received(1).StringGetAsync(fullKey);
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis cache hit for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetAsync_ShouldReturnDefault_WhenCacheKeyDoesNotExist(string cacheKey, string methodName)
        {
            // Arrange
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            _database.StringGetAsync(fullKey).Returns(RedisValue.Null);

            // Act
            var result = await _cacheService.GetAsync<string>(cacheKey);

            // Assert
            Assert.Null(result);
            await _database.Received(1).StringGetAsync(fullKey);
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis cache miss for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetAsync_ShouldReturnDefault_WhenRedisExceptionOccurs(string cacheKey, string methodName)
        {
            // Arrange
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            _database.StringGetAsync(fullKey).Returns(Task.FromException<RedisValue>(new RedisException("Redis connection failed")));

            // Act
            var result = await _cacheService.GetAsync<string>(cacheKey);

            // Assert
            Assert.Null(result);
            await _database.Received(1).StringGetAsync(fullKey);
            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis error occurred for key: {fullKey}. Returning default value")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetAsync_ShouldReturnDefault_WhenJsonExceptionOccurs(string cacheKey, string methodName)
        {
            // Arrange
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var invalidJson = "invalid json";
            _database.StringGetAsync(fullKey).Returns(new RedisValue(invalidJson));

            // Act
            var result = await _cacheService.GetAsync<string>(cacheKey);

            // Assert
            Assert.Null(result);
            await _database.Received(1).StringGetAsync(fullKey);
            await _database.Received(1).KeyDeleteAsync(fullKey);
            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"JSON deserialization error for key: {fullKey}. Removing invalid cache entry")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Fact]
        public void CacheType_ShouldReturnCorrectType()
        {
            // Act
            var cacheType = _cacheService.CacheType;

            // Assert
            Assert.Equal(typeof(Interfaces.IRedisCacheType), cacheType);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldAcquireLockAndCache_WhenCacheMissOccurs(string cacheKey, string methodName)
        {
            // Arrange
            var expectedValue = _fixture.Create<string>();
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var lockKey = $"{fullKey}:lock";

            _database.StringGetAsync(fullKey).Returns(RedisValue.Null, RedisValue.Null);
            
            // Lock acquisition succeeds
            _database.StringSetAsync(
                Arg.Is<RedisKey>(k => k.ToString() == lockKey), 
                Arg.Any<RedisValue>(), 
                Arg.Any<TimeSpan>(), 
                When.NotExists)
                .Returns(true);
            
            _database.StringSetAsync(fullKey, Arg.Any<RedisValue>(), Arg.Any<TimeSpan>())
                .Returns(Task.FromResult(true));

            // Mock Lua script execution for lock release (return 1 = success)
            _database.ScriptEvaluateAsync(
                Arg.Any<string>(),
                Arg.Any<RedisKey[]>(),
                Arg.Any<RedisValue[]>())
                .Returns(RedisResult.Create(1));

            // Act
            var result = await _cacheService.GetOrAddAsync(cacheKey, () => Task.FromResult(expectedValue), methodName);

            // Assert
            Assert.Equal(expectedValue, result);
            
            // Verify lock was acquired
            await _database.Received(1).StringSetAsync(
                Arg.Is<RedisKey>(k => k.ToString() == lockKey), 
                Arg.Any<RedisValue>(), 
                Arg.Any<TimeSpan>(), 
                When.NotExists);
            
            // Verify lock was released using Lua script
            await _database.Received(1).ScriptEvaluateAsync(
                Arg.Any<string>(),
                Arg.Is<RedisKey[]>(keys => keys.Length == 1 && keys[0].ToString() == lockKey),
                Arg.Any<RedisValue[]>());
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldReturnCachedValue_WhenAnotherThreadPopulatesCacheWhileWaitingForLock(string cacheKey, string methodName)
        {
            // Arrange
            var cachedValue = _fixture.Create<string>();
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var lockKey = $"{fullKey}:lock";
            var serializedValue = JsonSerializer.Serialize(cachedValue);

            // First check: cache miss, second check after lock: cache hit
            _database.StringGetAsync(fullKey).Returns(
                RedisValue.Null, 
                new RedisValue(serializedValue));
            
            // Lock acquisition succeeds
            _database.StringSetAsync(
                Arg.Is<RedisKey>(k => k.ToString() == lockKey), 
                Arg.Any<RedisValue>(), 
                Arg.Any<TimeSpan>(), 
                When.NotExists)
                .Returns(true);

            // Mock Lua script execution for lock release
            _database.ScriptEvaluateAsync(
                Arg.Any<string>(),
                Arg.Any<RedisKey[]>(),
                Arg.Any<RedisValue[]>())
                .Returns(RedisResult.Create(1));

            // Act
            var result = await _cacheService.GetOrAddAsync(cacheKey, () => Task.FromResult("should-not-be-called"), methodName);

            // Assert
            Assert.Equal(cachedValue, result);
            
            // Should not call StringSetAsync for the actual cache key since value was populated
            await _database.DidNotReceive().StringSetAsync(
                Arg.Is<RedisKey>(k => k.ToString() == fullKey), 
                Arg.Any<RedisValue>(), 
                Arg.Any<TimeSpan>());
            
            // Verify double-check pattern logged
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis cache hit after lock acquisition for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldWaitAndRetry_WhenLockAcquisitionFails(string cacheKey, string methodName)
        {
            // Arrange
            var cachedValue = _fixture.Create<string>();
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var lockKey = $"{fullKey}:lock";
            var serializedValue = JsonSerializer.Serialize(cachedValue);

            // First check: cache miss
            // Subsequent checks: null for first 2 retries, then cache hit on 3rd retry
            _database.StringGetAsync(fullKey).Returns(
                RedisValue.Null,  // Initial check
                RedisValue.Null,  // 1st retry
                RedisValue.Null,  // 2nd retry
                new RedisValue(serializedValue)); // 3rd retry - cache hit
            
            // Lock acquisition fails (another thread has the lock)
            _database.StringSetAsync(
                Arg.Is<RedisKey>(k => k.ToString() == lockKey), 
                Arg.Any<RedisValue>(), 
                Arg.Any<TimeSpan>(), 
                When.NotExists)
                .Returns(false);

            // Act
            var result = await _cacheService.GetOrAddAsync(cacheKey, () => Task.FromResult("should-not-be-called"), methodName);

            // Assert
            Assert.Equal(cachedValue, result);
            
            // Verify it retried reading from cache multiple times
            await _database.Received(4).StringGetAsync(fullKey);
            
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Waiting for another thread to populate cache for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
            
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis cache hit after waiting for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldFallbackToFetchFunction_WhenMaxRetriesReachedAndCacheStillEmpty(string cacheKey, string methodName)
        {
            // Arrange
            var expectedValue = _fixture.Create<string>();
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var lockKey = $"{fullKey}:lock";

            // Always return null (cache never gets populated)
            _database.StringGetAsync(fullKey).Returns(RedisValue.Null);
            
            // Lock acquisition fails
            _database.StringSetAsync(
                Arg.Is<RedisKey>(k => k.ToString() == lockKey), 
                Arg.Any<RedisValue>(), 
                Arg.Any<TimeSpan>(), 
                When.NotExists)
                .Returns(false);

            // Act
            var result = await _cacheService.GetOrAddAsync(cacheKey, () => Task.FromResult(expectedValue), methodName);

            // Assert
            Assert.Equal(expectedValue, result);
            
            _logger.Received(1).Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Max retries reached waiting for cache population for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldReleaseLock_WhenExceptionOccursDuringFetch(string cacheKey, string methodName)
        {
            // Arrange
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var lockKey = $"{fullKey}:lock";

            _database.StringGetAsync(fullKey).Returns(RedisValue.Null, RedisValue.Null);
            
            // Lock acquisition succeeds
            _database.StringSetAsync(
                Arg.Is<RedisKey>(k => k.ToString() == lockKey), 
                Arg.Any<RedisValue>(), 
                Arg.Any<TimeSpan>(), 
                When.NotExists)
                .Returns(true);

            // Mock Lua script execution for lock release
            _database.ScriptEvaluateAsync(
                Arg.Any<string>(),
                Arg.Any<RedisKey[]>(),
                Arg.Any<RedisValue[]>())
                .Returns(RedisResult.Create(1));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await _cacheService.GetOrAddAsync(cacheKey, () => Task.FromException<string>(new InvalidOperationException("Fetch failed")), methodName));

            // Verify lock was still released even though exception occurred
            await _database.Received(1).ScriptEvaluateAsync(
                Arg.Any<string>(),
                Arg.Is<RedisKey[]>(keys => keys.Length == 1 && keys[0].ToString() == lockKey),
                Arg.Any<RedisValue[]>());
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldRespectCancellationToken_WhenCancelled(string cacheKey, string methodName)
        {
            // Arrange
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            _database.StringGetAsync(fullKey).Returns(RedisValue.Null);

            // Act & Assert
            // TaskCanceledException is a subclass of OperationCanceledException
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => 
                await _cacheService.GetOrAddAsync(cacheKey, () => Task.FromResult("value"), methodName, cts.Token));
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldCancelDuringWait_WhenCancellationTokenTriggered(string cacheKey, string methodName)
        {
            // Arrange
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var lockKey = $"{fullKey}:lock";
            var cts = new CancellationTokenSource();

            _database.StringGetAsync(fullKey).Returns(RedisValue.Null);
            
            // Lock acquisition fails (simulating waiting scenario)
            _database.StringSetAsync(
                Arg.Is<RedisKey>(k => k.ToString() == lockKey), 
                Arg.Any<RedisValue>(), 
                Arg.Any<TimeSpan>(), 
                When.NotExists)
                .Returns(false);

            // Cancel after a short delay
            _ = Task.Run(async () => 
            {
                await Task.Delay(100);
                cts.Cancel();
            });

            // Act & Assert
            // TaskCanceledException is a subclass of OperationCanceledException
            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => 
                await _cacheService.GetOrAddAsync(cacheKey, () => Task.FromResult("value"), methodName, cts.Token));
            
            Assert.NotNull(exception);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldHandleConcurrentRequests_WithoutDuplicateFetches(string cacheKey, string methodName)
        {
            // Arrange
            var expectedValue = _fixture.Create<string>();
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var lockKey = $"{fullKey}:lock";
            var fetchCallCount = 0;
            var serializedValue = JsonSerializer.Serialize(expectedValue, _jsonSerializerOptions);

            // Setup: First request gets cache miss, then after it populates, subsequent requests get cache hit
            var cacheGetCallCount = 0;
            _database.StringGetAsync(fullKey).Returns(callInfo =>
            {
                var count = Interlocked.Increment(ref cacheGetCallCount);
                // First few calls return null (cache miss), then return the cached value
                return count <= 2 ? RedisValue.Null : new RedisValue(serializedValue);
            });
            
            // First thread acquires lock, others fail
            var lockCallCount = 0;
            _database.StringSetAsync(
                Arg.Is<RedisKey>(k => k.ToString() == lockKey), 
                Arg.Any<RedisValue>(), 
                Arg.Any<TimeSpan>(), 
                When.NotExists)
                .Returns(callInfo => 
                {
                    var acquired = Interlocked.Increment(ref lockCallCount) == 1;
                    return acquired;
                });
            
            // Cache set succeeds - this simulates the first thread populating the cache
            _database.StringSetAsync(fullKey, Arg.Any<RedisValue>(), Arg.Any<TimeSpan>())
                .Returns(callInfo =>
                {
                    // After cache is set, subsequent StringGetAsync calls should return the value
                    return Task.FromResult(true);
                });

            // Mock Lua script execution for lock release
            _database.ScriptEvaluateAsync(
                Arg.Any<string>(),
                Arg.Any<RedisKey[]>(),
                Arg.Any<RedisValue[]>())
                .Returns(RedisResult.Create(1));

            Func<Task<string>> fetchFunction = async () =>
            {
                Interlocked.Increment(ref fetchCallCount);
                await Task.Delay(10); // Small delay to simulate work
                return expectedValue;
            };

            // Act - simulate multiple concurrent requests
            var tasks = Enumerable.Range(0, 3).Select(_ => 
                _cacheService.GetOrAddAsync(cacheKey, fetchFunction, methodName)
            ).ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            // Only one fetch should have occurred (the one that got the lock)
            Assert.Equal(1, fetchCallCount);
            Assert.All(results, r => Assert.Equal(expectedValue, r));
        }

        [Theory]
        [CustomAutoData()]
        public async Task SetRawAsync_ShouldStoreRawByteData_WhenCalled(string cacheKey)
        {
            // Arrange
            var rawData = _fixture.Create<byte[]>();
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var expiry = TimeSpan.FromMinutes(10);

            // Act
            await _cacheService.SetRawAsync(cacheKey, rawData, expiry);

            // Assert
            await _database.Received(1).StringSetAsync(fullKey, rawData, expiry);
            _logger.Received(1).Log(
                LogLevel.Debug,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis raw data set for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task SetRawAsync_ShouldThrowRedisException_WhenRedisFailsToSet(string cacheKey)
        {
            // Arrange
            var rawData = _fixture.Create<byte[]>();
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            var expiry = TimeSpan.FromMinutes(10);

            _database.When(x => x.StringSetAsync(fullKey, Arg.Any<RedisValue>(), expiry))
                .Do(x => throw new RedisException("Redis connection failed"));

            // Act & Assert
            await Assert.ThrowsAsync<RedisException>(async () => 
                await _cacheService.SetRawAsync(cacheKey, rawData, expiry));

            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis error occurred while setting raw data for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetRawAsync_ShouldReturnRawByteData_WhenDataExists(string cacheKey)
        {
            // Arrange
            var rawData = _fixture.Create<byte[]>();
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";

            _database.StringGetAsync(fullKey).Returns((RedisValue)rawData);

            // Act
            var result = await _cacheService.GetRawAsync(cacheKey);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(rawData, result);
            _logger.Received(1).Log(
                LogLevel.Debug,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis raw data retrieved for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetRawAsync_ShouldReturnNull_WhenDataDoesNotExist(string cacheKey)
        {
            // Arrange
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            _database.StringGetAsync(fullKey).Returns(RedisValue.Null);

            // Act
            var result = await _cacheService.GetRawAsync(cacheKey);

            // Assert
            Assert.Null(result);
            _logger.Received(1).Log(
                LogLevel.Debug,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis cache miss for raw key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetRawAsync_ShouldReturnNull_WhenRedisExceptionOccurs(string cacheKey)
        {
            // Arrange
            var fullKey = $"{_redisCacheSettings.KeyPrefix}{cacheKey}";
            _database.StringGetAsync(fullKey).Returns(Task.FromException<RedisValue>(new RedisException("Redis connection failed")));

            // Act
            var result = await _cacheService.GetRawAsync(cacheKey);

            // Assert
            Assert.Null(result);
            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Redis error occurred while getting raw data for key: {fullKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }
    }
}