using FluentAssertions;
using GovUK.Dfe.CoreLibs.Http.NoScriptDetection;
using GovUK.Dfe.CoreLibs.Http.NoScriptDetection.Pixel;
using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.Http.Tests.NoScriptDetection
{
    public class NoScriptDetectionServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddNoScriptDetection_ShouldReturnServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddNoScriptDetection();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(services);
        }

        [Fact]
        public void AddNoScriptDetection_ShouldRegisterINoScriptPixelProvider()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNoScriptDetection();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var provider = serviceProvider.GetService<INoScriptPixelProvider>();
            provider.Should().NotBeNull();
        }

        [Fact]
        public void AddNoScriptDetection_ShouldRegisterTransparentPixelProvider()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNoScriptDetection();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var provider = serviceProvider.GetService<INoScriptPixelProvider>();
            provider.Should().BeOfType<TransparentPixelProvider>();
        }

        [Fact]
        public void AddNoScriptDetection_ShouldRegisterAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNoScriptDetection();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var provider1 = serviceProvider.GetService<INoScriptPixelProvider>();
            var provider2 = serviceProvider.GetService<INoScriptPixelProvider>();
            provider1.Should().BeSameAs(provider2);
        }

        [Fact]
        public void AddNoScriptDetection_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var act = () =>
            {
                services.AddNoScriptDetection();
                services.AddNoScriptDetection();
            };

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void AddNoScriptDetection_CalledMultipleTimes_ShouldRegisterMultipleProviders()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNoScriptDetection();
            services.AddNoScriptDetection();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var providers = serviceProvider.GetServices<INoScriptPixelProvider>().ToList();
            providers.Should().HaveCount(2);
        }
    }
}

