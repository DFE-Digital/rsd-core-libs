using GovUK.Dfe.CoreLibs.Utilities.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.Utilities.Tests.RateLimiting
{
    public class RateLimitStoreTests
    {
        [Fact]
        public void Logs_IsEmptyOnInitialization()
        {
            var store = new RateLimitStore<Guid>();
            Assert.Empty(store.Logs);
        }
    }

    public class RateLimiterFactoryTests
    {
        [Fact]
        public void Create_WithValidParameters_ReturnsLimiter()
        {
            var store = new RateLimitStore<Guid>();
            var factory = new RateLimiterFactory<Guid>(store, () => DateTime.UtcNow);
            var limiter = factory.Create(1, TimeSpan.FromSeconds(1));

            Assert.NotNull(limiter);
            Assert.IsType<TimeBasedRateLimiter<Guid>>(limiter);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        public void Create_WithInvalidParameters_ThrowsArgumentOutOfRangeException(int maxRequests, int intervalSeconds)
        {
            var store = new RateLimitStore<Guid>();
            var factory = new RateLimiterFactory<Guid>(store, () => DateTime.UtcNow);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => factory.Create(maxRequests, TimeSpan.FromSeconds(intervalSeconds)));
        }
    }

    public class TimeBasedRateLimiterTests
    {
        [Fact]
        public void IsAllowed_AllowsUpToMaxRequests_ThenDenies()
        {
            var current = DateTime.UtcNow;
            Func<DateTime> timeProvider = () => current;
            var store = new RateLimitStore<Guid>();
            var limiter = new TimeBasedRateLimiter<Guid>(2, TimeSpan.FromSeconds(1), store, timeProvider);
            var key = Guid.NewGuid();

            // Two allowed
            Assert.True(limiter.IsAllowed(key));
            Assert.True(limiter.IsAllowed(key));
            // Third denied
            Assert.False(limiter.IsAllowed(key));
        }

        [Fact]
        public void IsAllowed_AllowsAfterIntervalExpires()
        {
            var current = DateTime.UtcNow;
            Func<DateTime> timeProvider = () => current;
            var store = new RateLimitStore<Guid>();
            var limiter = new TimeBasedRateLimiter<Guid>(1, TimeSpan.FromSeconds(1), store, timeProvider);
            var key = Guid.NewGuid();

            Assert.True(limiter.IsAllowed(key));
            Assert.False(limiter.IsAllowed(key));

            // Advance time beyond the interval
            current = current.AddSeconds(1);
            Assert.True(limiter.IsAllowed(key));
        }
    }

    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddRateLimiting_RegistersStoreAndFactory()
        {
            var services = new ServiceCollection();
            services.AddRateLimiting<Guid>();
            var provider = services.BuildServiceProvider();

            var store = provider.GetService<RateLimitStore<Guid>>();
            var factory = provider.GetService<IRateLimiterFactory<Guid>>();

            Assert.NotNull(store);
            Assert.NotNull(factory);
            // Factory should produce working limiter
            var limiter = factory.Create(1, TimeSpan.FromSeconds(1));
            Assert.True(limiter.IsAllowed(Guid.NewGuid()));
        }
    }

}
