using DfE.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using DfE.CoreLibs.Security.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DfE.CoreLibs.Security.Cypress;
using DfE.CoreLibs.Security.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Configuration;

namespace DfE.CoreLibs.Security.Tests.Antiforgery
{
    public class CustomAntiForgeryExtensionsTests
    {
        [Fact]
        public void AddCustomAntiForgeryHandling_RegistersRequiredServicesWithConfiguration()
        {
            // Arrange  
            var services = new ServiceCollection();
            services.AddSingleton(Substitute.For<IHostEnvironment>());
            services.AddSingleton(Substitute.For<IConfiguration>());

            var mvcBuilder = Substitute.For<IMvcBuilder>();
            mvcBuilder.Services.Returns(services);

            var customChecker = Substitute.For<ICustomRequestChecker>();
            mvcBuilder.Services.AddCustomRequestCheckerProvider<CypressRequestChecker>();

            static void configure(CustomAwareAntiForgeryOptions options)
            {
                options.CheckerGroups =
                [
                    new CheckerGroup
                    {
                         TypeNames = [nameof(CypressRequestChecker)],
                         CheckerOperator = CheckerOperator.Or
                    }
                ];
            }

            // Act  
            var result = CustomAntiForgeryExtensions.AddCustomAntiForgeryHandling(mvcBuilder, configure);

            // Assert  

            Assert.Contains(services, d => d.ServiceType == typeof(ICustomRequestChecker) && (d.ImplementationType == typeof(CypressRequestChecker) || d.ImplementationFactory == null));

            Assert.Contains(services, d => d.ServiceType == typeof(CustomAwareAntiForgeryFilter));

            Assert.Same(mvcBuilder, result);

            // Register MvcOptions so PostConfigure works  
            services.AddOptions<MvcOptions>();

            var serviceProvider = services.BuildServiceProvider();
            var mvcOptions = serviceProvider.GetRequiredService<IOptions<MvcOptions>>().Value;

            var configuredOptions = serviceProvider.GetRequiredService<IOptions<CustomAwareAntiForgeryOptions>>().Value;
            Assert.NotNull(configuredOptions.CheckerGroups);
            Assert.Single(configuredOptions.CheckerGroups);
            Assert.Equal(nameof(CypressRequestChecker), configuredOptions.CheckerGroups[0].TypeNames[0]);
            Assert.Equal(CheckerOperator.Or, configuredOptions.CheckerGroups[0].CheckerOperator);

            Assert.Contains(mvcOptions.Filters, filter => filter is ServiceFilterAttribute serviceFilter && serviceFilter.ServiceType == typeof(CustomAwareAntiForgeryFilter));
            Assert.Contains(services, d => d.ServiceType == typeof(ICustomRequestChecker) && (d.ImplementationType == typeof(CypressRequestChecker) || d.ImplementationFactory == null));

            Assert.Contains(services, d => d.ServiceType == typeof(CustomAwareAntiForgeryFilter));

            Assert.Same(mvcBuilder, result);

            using var scope = serviceProvider.CreateScope();
            var checkerList = scope.ServiceProvider.GetService<List<ICustomRequestChecker>>();

            // Assert
            Assert.NotNull(checkerList);
            Assert.Single(checkerList);
            Assert.Contains(checkerList, c => c.GetType() == typeof(CypressRequestChecker));
        }

        [Fact]
        public void AddCustomAntiForgeryHandling_RegistersRequiredServicesWithoutConfiguration()
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

            var configuredOptions = serviceProvider.GetRequiredService<IOptions<CustomAwareAntiForgeryOptions>>().Value;
            Assert.NotNull(configuredOptions.CheckerGroups);
            Assert.True(configuredOptions.CheckerGroups.Count == 0);

            Assert.Contains(mvcOptions.Filters, filter => filter is ServiceFilterAttribute serviceFilter && serviceFilter.ServiceType == typeof(CustomAwareAntiForgeryFilter));
            Assert.Contains(services, d => d.ServiceType == typeof(ICustomRequestChecker) && (d.ImplementationType == typeof(CypressRequestChecker) || d.ImplementationFactory == null));

            Assert.Contains(services, d => d.ServiceType == typeof(CustomAwareAntiForgeryFilter));

            Assert.Same(mvcBuilder, result);
        }
    }
}
