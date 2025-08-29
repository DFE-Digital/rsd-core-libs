using System.Net;
using System.Text.Json;
using AutoFixture;
using DfE.CoreLibs.Caching.Services;
using DfE.CoreLibs.Caching.Settings;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;

namespace DfE.CoreLibs.Caching.Tests.Services
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
            
            _database.StringGetAsync(fullKey).Returns(RedisValue.Null);
            _database.StringSetAsync(fullKey, Arg.Any<RedisValue>(), TimeSpan.FromSeconds(_redisCacheSettings.DefaultDurationInSeconds))
                .Returns(Task.FromResult(true));

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
            var expectedDuration = TimeSpan.FromSeconds(_redisCacheSettings.Durations[methodName]);
            
            _database.StringGetAsync(fullKey).Returns(RedisValue.Null);
            _database.StringSetAsync(fullKey, Arg.Any<RedisValue>(), expectedDuration)
                .Returns(Task.FromResult(true));

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
            
            _database.StringGetAsync(fullKey).Returns(RedisValue.Null);

            // Act
            var result = await _cacheService.GetOrAddAsync<string>(cacheKey, () => Task.FromResult<string>(null!), methodName);

            // Assert
            Assert.Null(result);
            await _database.DidNotReceive().StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan>());
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
    }
}
