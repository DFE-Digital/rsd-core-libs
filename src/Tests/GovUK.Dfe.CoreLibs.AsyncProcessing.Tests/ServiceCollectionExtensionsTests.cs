using GovUK.Dfe.CoreLibs.AsyncProcessing.Configurations;
using GovUK.Dfe.CoreLibs.AsyncProcessing.Interfaces;
using GovUK.Dfe.CoreLibs.AsyncProcessing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.AsyncProcessing.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddBackgroundService_ShouldRegisterRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddBackgroundService();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetService<IBackgroundServiceFactory>();
            var hostedService = serviceProvider.GetServices<IHostedService>().FirstOrDefault();

            Assert.NotNull(factory);
            Assert.NotNull(hostedService);
            Assert.IsType<BackgroundServiceFactory>(factory);
        }

        [Fact]
        public void AddBackgroundService_WithOptions_ShouldConfigureOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddBackgroundService(options =>
            {
                options.UseGlobalStoppingToken = true;
                options.MaxConcurrentWorkers = 5;
                options.ChannelCapacity = 100;
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<BackgroundServiceOptions>>().Value;

            Assert.True(options.UseGlobalStoppingToken);
            Assert.Equal(5, options.MaxConcurrentWorkers);
            Assert.Equal(100, options.ChannelCapacity);
        }

        [Fact]
        public void AddBackgroundService_WithNullOptions_ShouldUseDefaults()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddBackgroundService(null);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<BackgroundServiceOptions>>().Value;

            Assert.False(options.UseGlobalStoppingToken);
            Assert.Equal(1, options.MaxConcurrentWorkers);
            Assert.Equal(int.MaxValue, options.ChannelCapacity);
        }

        [Fact]
        public void AddBackgroundServiceWithParallelism_ShouldConfigureParallelProcessing()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddBackgroundServiceWithParallelism(maxConcurrentWorkers: 8);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<BackgroundServiceOptions>>().Value;

            Assert.Equal(8, options.MaxConcurrentWorkers);
            Assert.True(options.UseGlobalStoppingToken);
            Assert.Equal(int.MaxValue, options.ChannelCapacity);
        }

        [Fact]
        public void AddBackgroundServiceWithParallelism_WithCapacity_ShouldConfigureBoundedChannel()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddBackgroundServiceWithParallelism(maxConcurrentWorkers: 4, channelCapacity: 50);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<BackgroundServiceOptions>>().Value;

            Assert.Equal(4, options.MaxConcurrentWorkers);
            Assert.Equal(50, options.ChannelCapacity);
            Assert.True(options.UseGlobalStoppingToken);
        }

        [Fact]
        public void AddBackgroundServiceWithParallelism_WithDefaultParameters_ShouldUseDefaults()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddBackgroundServiceWithParallelism();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<BackgroundServiceOptions>>().Value;

            Assert.Equal(4, options.MaxConcurrentWorkers);
            Assert.Equal(int.MaxValue, options.ChannelCapacity);
            Assert.True(options.UseGlobalStoppingToken);
        }

        [Fact]
        public void AddBackgroundService_ShouldRegisterSingletonFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddBackgroundService();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var factory1 = serviceProvider.GetRequiredService<IBackgroundServiceFactory>();
            var factory2 = serviceProvider.GetRequiredService<IBackgroundServiceFactory>();

            Assert.Same(factory1, factory2);
        }

        [Fact]
        public void AddBackgroundService_ShouldRegisterHostedServiceUsingFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddBackgroundService();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IBackgroundServiceFactory>();
            var hostedServices = serviceProvider.GetServices<IHostedService>().ToList();

            Assert.Single(hostedServices);
            Assert.Same(factory, hostedServices[0]);
        }
    }
}

