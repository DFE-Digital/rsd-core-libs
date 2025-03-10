using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DfE.CoreLibs.Security.Cypress;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace DfE.CoreLibs.Security.Tests.CypressTests
{
    public class CypressAntiForgeryExtensionsTests
    {
        private readonly IFixture _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

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
            Assert.Contains(services, d => d.ServiceType == typeof(ICypressRequestChecker) && d.ImplementationType == typeof(CypressRequestChecker));

            Assert.Contains(services, d => d.ServiceType == typeof(CypressAwareAntiForgeryFilter));

            Assert.Same(mvcBuilder, result);
        }
    }
}