using DfE.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using DfE.CoreLibs.Security.Antiforgery;

namespace DfE.CoreLibs.Security.Tests.Antiforgery
{
    public class CustomAntiForgeryExtensionsTests
    {
        [Fact]
        public void AddCustomAntiForgeryHandling_RegistersRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var mvcBuilder = Substitute.For<IMvcBuilder>();
            mvcBuilder.Services.Returns(services);

            // Act
            var result = CustomAntiForgeryExtensions.AddCustomAntiForgeryHandling(mvcBuilder);

            // Assert
            Assert.Contains(services, d => d.ServiceType == typeof(ICustomRequestChecker) && d.ImplementationType == typeof(CustomRequestChecker));

            Assert.Contains(services, d => d.ServiceType == typeof(CustomAwareAntiForgeryFilter));

            Assert.Same(mvcBuilder, result);
        }
    }
}
