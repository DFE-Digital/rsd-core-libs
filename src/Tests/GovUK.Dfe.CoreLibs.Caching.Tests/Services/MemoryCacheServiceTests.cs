using AutoFixture;
using GovUK.Dfe.CoreLibs.Caching.Services;
using GovUK.Dfe.CoreLibs.Caching.Settings;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GovUK.Dfe.CoreLibs.Caching.Tests.Services
{
    public class MemoryCacheServiceTests
    {
        private readonly IFixture _fixture;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly IOptions<CacheSettings> _options;
        private readonly MemoryCacheService _cacheService;
        private readonly MemoryCacheSettings _cacheSettings;

        public MemoryCacheServiceTests()
        {
            _fixture = new Fixture();
            _memoryCache = Substitute.For<IMemoryCache>();
            _logger = Substitute.For<ILogger<MemoryCacheService>>();

            _cacheSettings = new MemoryCacheSettings { DefaultDurationInSeconds = 5, Durations = new Dictionary<string, int> { { "TestMethod", 10 } } };
            var settings = new CacheSettings { Memory = _cacheSettings };
            _options = Options.Create(settings);

            _cacheService = new MemoryCacheService(_memoryCache, _logger, _options);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldReturnCachedValue_WhenCacheKeyExists(string cacheKey, string methodName)
        {
            // Arrange
            var cachedValue = _fixture.Create<string>();
            _memoryCache.TryGetValue(cacheKey, out Arg.Any<object>()!).Returns(x =>
            {
                x[1] = cachedValue;
                return true;
            });

            // Act
            var result = await _cacheService.GetOrAddAsync(cacheKey, () => Task.FromResult(cachedValue), methodName);

            // Assert
            Assert.Equal(cachedValue, result);
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Cache hit for key: {cacheKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetOrAddAsync_ShouldFetchAndCacheValue_WhenCacheKeyDoesNotExist(string cacheKey, string methodName)
        {
            // Arrange
            var expectedValue = _fixture.Create<string>();
            _memoryCache.TryGetValue(cacheKey, out Arg.Any<object>()).Returns(false);

            // Act
            var result = await _cacheService.GetOrAddAsync(cacheKey, () => Task.FromResult(expectedValue), methodName);

            // Assert
            Assert.Equal(expectedValue, result);
            _memoryCache.Received(1).Set(cacheKey, expectedValue, TimeSpan.FromSeconds(_cacheSettings.DefaultDurationInSeconds));
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Cache miss for key: {cacheKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Cached result for key: {cacheKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public void Remove_ShouldRemoveValueFromCache_WhenCalled(string cacheKey)
        {
            // Act
            _cacheService.Remove(cacheKey);

            // Assert
            _memoryCache.Received(1).Remove(cacheKey);
            _logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Cache removed for key: {cacheKey}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }
    }
}
