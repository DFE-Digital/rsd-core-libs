using AutoFixture;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Caching.Services;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StackExchange.Redis;

namespace GovUK.Dfe.CoreLibs.Caching.Tests.Services
{
    public class SessionDistributedCacheAdapterTests
    {
        private readonly IFixture _fixture;
        private readonly IAdvancedRedisCacheService _cacheService;
        private readonly ILogger<DistributedCacheAdapter> _logger;
        private readonly SessionDistributedCacheAdapter _adapter;

        public SessionDistributedCacheAdapterTests()
        {
            _fixture = new Fixture();
            _cacheService = Substitute.For<IAdvancedRedisCacheService>();
            _logger = Substitute.For<ILogger<DistributedCacheAdapter>>();
            _adapter = new SessionDistributedCacheAdapter(_cacheService, _logger);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetAsync_ShouldReturnData_WhenKeyExists(string key)
        {
            var expectedData = _fixture.Create<byte[]>();
            _cacheService.GetRawAsync(key).Returns(expectedData);

            var result = await _adapter.GetAsync(key);

            Assert.Equal(expectedData, result);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetAsync_ShouldReturnNull_WhenKeyDoesNotExist(string key)
        {
            _cacheService.GetRawAsync(key).Returns((byte[]?)null);

            var result = await _adapter.GetAsync(key);

            Assert.Null(result);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetAsync_ShouldLogAndRethrow_WhenReadFails(string key)
        {
            _cacheService.GetRawAsync(key).Returns(Task.FromException<byte[]?>(new RedisException("Redis connection failed")));

            await Assert.ThrowsAsync<RedisException>(() => _adapter.GetAsync(key));

            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Error getting distributed cache value for key: {key}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public void Get_ShouldRethrow_WhenReadTimesOut(string key)
        {
            _cacheService.GetRawAsync(key).Returns(Task.FromException<byte[]?>(new TimeoutException("Redis timeout")));

            Assert.Throws<TimeoutException>(() => _adapter.Get(key));
        }

        [Theory]
        [CustomAutoData()]
        public async Task RefreshAsync_ShouldNotThrow_WhenReadFails(string key)
        {
            _cacheService.GetRawAsync(key).Returns(Task.FromException<byte[]?>(new RedisException("Redis connection failed")));

            await _adapter.RefreshAsync(key);

            await _cacheService.DidNotReceive().SetRawAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<TimeSpan>());
        }
    }
}
