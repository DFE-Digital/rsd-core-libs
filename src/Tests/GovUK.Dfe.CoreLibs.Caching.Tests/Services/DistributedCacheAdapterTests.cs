using AutoFixture;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Caching.Services;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GovUK.Dfe.CoreLibs.Caching.Tests.Services
{
    public class DistributedCacheAdapterTests
    {
        private readonly IFixture _fixture;
        private readonly IAdvancedRedisCacheService _cacheService;
        private readonly ILogger<DistributedCacheAdapter> _logger;
        private readonly DistributedCacheAdapter _adapter;

        public DistributedCacheAdapterTests()
        {
            _fixture = new Fixture();
            _cacheService = Substitute.For<IAdvancedRedisCacheService>();
            _logger = Substitute.For<ILogger<DistributedCacheAdapter>>();
            _adapter = new DistributedCacheAdapter(_cacheService, _logger);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetAsync_ShouldReturnData_WhenKeyExists(string key)
        {
            // Arrange
            var expectedData = _fixture.Create<byte[]>();
            _cacheService.GetRawAsync(key).Returns(expectedData);

            // Act
            var result = await _adapter.GetAsync(key);

            // Assert
            Assert.Equal(expectedData, result);
            await _cacheService.Received(1).GetRawAsync(key);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetAsync_ShouldReturnNull_WhenKeyDoesNotExist(string key)
        {
            // Arrange
            _cacheService.GetRawAsync(key).Returns((byte[]?)null);

            // Act
            var result = await _adapter.GetAsync(key);

            // Assert
            Assert.Null(result);
            await _cacheService.Received(1).GetRawAsync(key);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetAsync_ShouldReturnNull_WhenExceptionOccurs(string key)
        {
            // Arrange
            _cacheService.GetRawAsync(key).Returns(Task.FromException<byte[]?>(new InvalidOperationException("Cache error")));

            // Act
            var result = await _adapter.GetAsync(key);

            // Assert
            Assert.Null(result);
            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Error getting distributed cache value for key: {key}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public void Get_ShouldReturnData_WhenKeyExists(string key)
        {
            // Arrange
            var expectedData = _fixture.Create<byte[]>();
            _cacheService.GetRawAsync(key).Returns(expectedData);

            // Act
            var result = _adapter.Get(key);

            // Assert
            Assert.Equal(expectedData, result);
        }

        [Theory]
        [CustomAutoData()]
        public async Task SetAsync_ShouldStoreData_WithAbsoluteExpirationRelativeToNow(string key)
        {
            // Arrange
            var data = _fixture.Create<byte[]>();
            var expiry = TimeSpan.FromMinutes(30);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };

            // Act
            await _adapter.SetAsync(key, data, options);

            // Assert
            await _cacheService.Received(1).SetRawAsync(key, data, expiry);
        }

        [Theory]
        [CustomAutoData()]
        public async Task SetAsync_ShouldStoreData_WithSlidingExpiration(string key)
        {
            // Arrange
            var data = _fixture.Create<byte[]>();
            var expiry = TimeSpan.FromMinutes(15);
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = expiry
            };

            // Act
            await _adapter.SetAsync(key, data, options);

            // Assert
            await _cacheService.Received(1).SetRawAsync(key, data, expiry);
        }

        [Theory]
        [CustomAutoData()]
        public async Task SetAsync_ShouldUseDefaultExpiry_WhenNoExpirationSpecified(string key)
        {
            // Arrange
            var data = _fixture.Create<byte[]>();
            var options = new DistributedCacheEntryOptions();

            // Act
            await _adapter.SetAsync(key, data, options);

            // Assert
            await _cacheService.Received(1).SetRawAsync(key, data, TimeSpan.FromMinutes(20));
        }

        [Theory]
        [CustomAutoData()]
        public async Task SetAsync_ShouldPrioritizeAbsoluteExpiration_OverSlidingExpiration(string key)
        {
            // Arrange
            var data = _fixture.Create<byte[]>();
            var absoluteExpiry = TimeSpan.FromMinutes(10);
            var slidingExpiry = TimeSpan.FromMinutes(5);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiry,
                SlidingExpiration = slidingExpiry
            };

            // Act
            await _adapter.SetAsync(key, data, options);

            // Assert
            await _cacheService.Received(1).SetRawAsync(key, data, absoluteExpiry);
        }

        [Theory]
        [CustomAutoData()]
        public async Task SetAsync_ShouldThrowException_WhenSetFails(string key)
        {
            // Arrange
            var data = _fixture.Create<byte[]>();
            var options = new DistributedCacheEntryOptions();
            _cacheService.SetRawAsync(key, data, Arg.Any<TimeSpan>())
                .Returns(Task.FromException(new InvalidOperationException("Set failed")));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await _adapter.SetAsync(key, data, options));

            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Error setting distributed cache value for key: {key}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public void Set_ShouldStoreData_WhenCalled(string key)
        {
            // Arrange
            var data = _fixture.Create<byte[]>();
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            // Act
            _adapter.Set(key, data, options);

            // Assert (verify async method was called)
            _cacheService.Received(1).SetRawAsync(key, data, Arg.Any<TimeSpan>());
        }

        [Theory]
        [CustomAutoData()]
        public async Task RefreshAsync_ShouldExtendExpiration_WhenKeyExists(string key)
        {
            // Arrange
            var existingData = _fixture.Create<byte[]>();
            _cacheService.GetRawAsync(key).Returns(existingData);

            // Act
            await _adapter.RefreshAsync(key);

            // Assert
            await _cacheService.Received(1).GetRawAsync(key);
            await _cacheService.Received(1).SetRawAsync(key, existingData, TimeSpan.FromMinutes(20));
        }

        [Theory]
        [CustomAutoData()]
        public async Task RefreshAsync_ShouldDoNothing_WhenKeyDoesNotExist(string key)
        {
            // Arrange
            _cacheService.GetRawAsync(key).Returns((byte[]?)null);

            // Act
            await _adapter.RefreshAsync(key);

            // Assert
            await _cacheService.Received(1).GetRawAsync(key);
            await _cacheService.DidNotReceive().SetRawAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<TimeSpan>());
        }

        [Theory]
        [CustomAutoData()]
        public async Task RefreshAsync_ShouldLogError_WhenExceptionOccurs(string key)
        {
            // Arrange
            _cacheService.GetRawAsync(key).Returns(Task.FromException<byte[]?>(new InvalidOperationException("Refresh failed")));

            // Act
            await _adapter.RefreshAsync(key);

            // Assert - The error is logged from GetAsync, not RefreshAsync
            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Error getting distributed cache value for key: {key}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public void Refresh_ShouldExtendExpiration_WhenCalled(string key)
        {
            // Arrange
            var existingData = _fixture.Create<byte[]>();
            _cacheService.GetRawAsync(key).Returns(existingData);

            // Act
            _adapter.Refresh(key);

            // Assert (verify async method was called)
            _cacheService.Received(1).GetRawAsync(key);
        }

        [Theory]
        [CustomAutoData()]
        public async Task RemoveAsync_ShouldRemoveKey_WhenCalled(string key)
        {
            // Act
            await _adapter.RemoveAsync(key);

            // Assert
            await _cacheService.Received(1).RemoveAsync(key);
        }

        [Theory]
        [CustomAutoData()]
        public async Task RemoveAsync_ShouldLogError_WhenExceptionOccurs(string key)
        {
            // Arrange
            _cacheService.RemoveAsync(key).Returns(Task.FromException(new InvalidOperationException("Remove failed")));

            // Act
            await _adapter.RemoveAsync(key);

            // Assert
            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains($"Error removing distributed cache key: {key}")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Theory]
        [CustomAutoData()]
        public void Remove_ShouldRemoveKey_WhenCalled(string key)
        {
            // Act
            _adapter.Remove(key);

            // Assert (verify async method was called)
            _cacheService.Received(1).RemoveAsync(key);
        }

        [Theory]
        [CustomAutoData()]
        public async Task GetAsync_ShouldRespectCancellationToken(string key)
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var expectedData = _fixture.Create<byte[]>();
            _cacheService.GetRawAsync(key).Returns(expectedData);

            // Act
            var result = await _adapter.GetAsync(key, cts.Token);

            // Assert
            Assert.Equal(expectedData, result);
            await _cacheService.Received(1).GetRawAsync(key);
        }

        [Theory]
        [CustomAutoData()]
        public async Task SetAsync_ShouldRespectCancellationToken(string key)
        {
            // Arrange
            var data = _fixture.Create<byte[]>();
            var options = new DistributedCacheEntryOptions();
            var cts = new CancellationTokenSource();

            // Act
            await _adapter.SetAsync(key, data, options, cts.Token);

            // Assert
            await _cacheService.Received(1).SetRawAsync(key, data, Arg.Any<TimeSpan>());
        }

        [Theory]
        [CustomAutoData()]
        public async Task RefreshAsync_ShouldRespectCancellationToken(string key)
        {
            // Arrange
            var existingData = _fixture.Create<byte[]>();
            _cacheService.GetRawAsync(key).Returns(existingData);
            var cts = new CancellationTokenSource();

            // Act
            await _adapter.RefreshAsync(key, cts.Token);

            // Assert
            await _cacheService.Received(1).GetRawAsync(key);
            await _cacheService.Received(1).SetRawAsync(key, existingData, TimeSpan.FromMinutes(20));
        }

        [Theory]
        [CustomAutoData()]
        public async Task RemoveAsync_ShouldRespectCancellationToken(string key)
        {
            // Arrange
            var cts = new CancellationTokenSource();

            // Act
            await _adapter.RemoveAsync(key, cts.Token);

            // Assert
            await _cacheService.Received(1).RemoveAsync(key);
        }
    }
}

