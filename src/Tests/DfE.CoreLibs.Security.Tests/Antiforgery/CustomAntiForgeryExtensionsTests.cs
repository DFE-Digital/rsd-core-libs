using DfE.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using DfE.CoreLibs.Security.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DfE.CoreLibs.Security.Cypress;

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

            var customChecker = Substitute.For<ICustomRequestChecker>();  
            mvcBuilder.Services.AddCustomRequestCheckerProvider<CypressRequestChecker>();

            // Act
            var result = CustomAntiForgeryExtensions.AddCustomAntiForgeryHandling(mvcBuilder);

            // Assert

            Assert.Contains(services, d => d.ServiceType == typeof(ICustomRequestChecker) && (d.ImplementationType == typeof(CypressRequestChecker) || d.ImplementationFactory == null)); 

            Assert.Contains(services, d => d.ServiceType == typeof(CustomAwareAntiForgeryFilter));

            Assert.Same(mvcBuilder, result);

            // Register MvcOptions so PostConfigure works
            services.AddOptions<MvcOptions>();

            var serviceProvider = services.BuildServiceProvider();
            var mvcOptions = serviceProvider.GetRequiredService<IOptions<MvcOptions>>().Value;

            Assert.Contains(mvcOptions.Filters, filter => filter is ServiceFilterAttribute serviceFilter && serviceFilter.ServiceType == typeof(CustomAwareAntiForgeryFilter));
            Assert.Contains(services, d => d.ServiceType == typeof(ICustomRequestChecker) && (d.ImplementationType == typeof(CypressRequestChecker) || d.ImplementationFactory == null));

            Assert.Contains(services, d => d.ServiceType == typeof(CustomAwareAntiForgeryFilter));

            Assert.Same(mvcBuilder, result); 
        }
    }
}
