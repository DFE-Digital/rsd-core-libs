using GovUK.Dfe.CoreLibs.Security.Cypress;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace GovUK.Dfe.CoreLibs.Security.Tests.CypressTests
{
    public class CypressAntiForgeryExtensionsTests
    {
        [Fact]
        public void AddCypressAntiForgeryHandling_RegistersRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var mvcBuilder = Substitute.For<IMvcBuilder>();
            mvcBuilder.Services.Returns(services);

            // Act
            var result = CypressAntiForgeryExtensions.AddCypressAntiForgeryHandling(mvcBuilder);

            // Assert
            Assert.Contains(services, d => d.ServiceType == typeof(ICustomRequestChecker) && d.ImplementationType == typeof(CypressRequestChecker));

            Assert.Contains(services, d => d.ServiceType == typeof(CypressAwareAntiForgeryFilter));

            Assert.Same(mvcBuilder, result);
        }
    }
}
