using DfE.CoreLibs.Http.Configuration;
using DfE.CoreLibs.Http.Extensions;
using DfE.CoreLibs.Http.Interfaces;
using DfE.CoreLibs.Http.Models;
using DfE.CoreLibs.Http.Utils;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using ExceptionHandlerOptions = DfE.CoreLibs.Http.Configuration.ExceptionHandlerOptions;

namespace DfE.CoreLibs.Http.Tests.Extensions
{
    public class ExceptionHandlerExtensionsTests
    {
        [Fact]
        public void UseGlobalExceptionHandler_WithoutOptions_ShouldReturnApplicationBuilder()
        {
            // Arrange
            var services = new ServiceCollection();
            services.Configure<ExceptionHandlerOptions>(options =>
            {
                options.IncludeDetails = true;
                options.DefaultErrorMessage = "Test error";
            });
            var serviceProvider = services.BuildServiceProvider();
            var app = Substitute.For<IApplicationBuilder>();
            app.ApplicationServices.Returns(serviceProvider);

            // Act
            var result = app.UseGlobalExceptionHandler();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IApplicationBuilder>();
        }

        [Fact]
        public void UseGlobalExceptionHandler_WithConfigureOptions_ShouldReturnApplicationBuilder()
        {
            // Arrange
            var app = Substitute.For<IApplicationBuilder>();

            // Act
            var result = app.UseGlobalExceptionHandler(options =>
            {
                options.IncludeDetails = true;
                options.DefaultErrorMessage = "Custom error";
            });

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IApplicationBuilder>();
        }

        [Fact]
        public void UseGlobalExceptionHandler_WithDirectOptions_ShouldReturnApplicationBuilder()
        {
            // Arrange
            var app = Substitute.For<IApplicationBuilder>();
            var options = new ExceptionHandlerOptions
            {
                IncludeDetails = true,
                DefaultErrorMessage = "Direct error"
            };

            // Act
            var result = app.UseGlobalExceptionHandler(options);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IApplicationBuilder>();
        }

        [Fact]
        public void ConfigureGlobalExceptionHandler_ShouldConfigureOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.ConfigureGlobalExceptionHandler(options =>
            {
                options.IncludeDetails = true;
                options.DefaultErrorMessage = "Configured error";
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var configuredOptions = serviceProvider.GetService<IOptions<ExceptionHandlerOptions>>()?.Value;
            
            configuredOptions.Should().NotBeNull();
            configuredOptions!.IncludeDetails.Should().BeTrue();
            configuredOptions.DefaultErrorMessage.Should().Be("Configured error");
        }

        [Fact]
        public void AddCustomExceptionHandler_WithType_ShouldRegisterHandler()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCustomExceptionHandler<TestCustomExceptionHandler>();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var handler = serviceProvider.GetService<ICustomExceptionHandler>();
            
            handler.Should().NotBeNull();
            handler.Should().BeOfType<TestCustomExceptionHandler>();
        }

        [Fact]
        public void AddCustomExceptionHandler_WithInstance_ShouldRegisterHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            var handler = new TestCustomExceptionHandler();

            // Act
            services.AddCustomExceptionHandler(handler);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var registeredHandler = serviceProvider.GetService<ICustomExceptionHandler>();
            
            registeredHandler.Should().NotBeNull();
            registeredHandler.Should().Be(handler);
        }

        [Fact]
        public void AddCustomExceptionHandlers_WithMultipleHandlers_ShouldRegisterAllHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var handler1 = new TestCustomExceptionHandler();
            var handler2 = new AnotherTestCustomExceptionHandler();

            // Act
            services.AddCustomExceptionHandlers(handler1, handler2);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var handlers = serviceProvider.GetServices<ICustomExceptionHandler>().ToList();
            
            handlers.Should().HaveCount(2);
            handlers.Should().Contain(handler1);
            handlers.Should().Contain(handler2);
        }

        [Fact]
        public void WithCustomErrorIdGenerator_ShouldSetGenerator()
        {
            // Arrange
            var options = new ExceptionHandlerOptions();
            var customId = "CUSTOM-123";

            // Act
            var result = options.WithCustomErrorIdGenerator(() => customId);

            // Assert
            result.Should().Be(options);
            options.ErrorIdGenerator.Should().NotBeNull();
            options.ErrorIdGenerator!().Should().Be(customId);
        }

        [Fact]
        public void WithTimestampBasedErrorIds_ShouldSetTimestampGenerator()
        {
            // Arrange
            var options = new ExceptionHandlerOptions();

            // Act
            var result = options.WithTimestampBasedErrorIds();

            // Assert
            result.Should().Be(options);
            options.ErrorIdGenerator.Should().NotBeNull();
            var generatedId = options.ErrorIdGenerator!();
            System.Text.RegularExpressions.Regex.IsMatch(generatedId, @"^\d{8}-\d{6}-\d{4}$").Should().BeTrue();
        }

        [Fact]
        public void WithGuidBasedErrorIds_ShouldSetGuidGenerator()
        {
            // Arrange
            var options = new ExceptionHandlerOptions();

            // Act
            var result = options.WithGuidBasedErrorIds();

            // Assert
            result.Should().Be(options);
            options.ErrorIdGenerator.Should().NotBeNull();
            var generatedId = options.ErrorIdGenerator!();
            System.Text.RegularExpressions.Regex.IsMatch(generatedId, @"^[a-f0-9]{8}$").Should().BeTrue();
        }

        [Fact]
        public void WithSequentialErrorIds_ShouldSetSequentialGenerator()
        {
            // Arrange
            var options = new ExceptionHandlerOptions();

            // Act
            var result = options.WithSequentialErrorIds();

            // Assert
            result.Should().Be(options);
            options.ErrorIdGenerator.Should().NotBeNull();
            var generatedId = options.ErrorIdGenerator!();
            System.Text.RegularExpressions.Regex.IsMatch(generatedId, @"^\d{13}$").Should().BeTrue();
        }

        [Theory]
        [InlineData("Development", "D")]
        [InlineData("Test", "T")]
        [InlineData("Production", "P")]
        [InlineData("UAT", "U")]
        [InlineData("QA", "Q")]
        public void WithEnvironmentAwareErrorIds_ShouldSetEnvironmentAwareGenerator(string environment, string expectedPrefix)
        {
            // Arrange
            var options = new ExceptionHandlerOptions();

            // Act
            var result = options.WithEnvironmentAwareErrorIds(environment);

            // Assert
            result.Should().Be(options);
            options.ErrorIdGenerator.Should().NotBeNull();
            var generatedId = options.ErrorIdGenerator!();
            System.Text.RegularExpressions.Regex.IsMatch(generatedId, $"^{expectedPrefix}-\\d{{6}}$").Should().BeTrue();
        }

        [Theory]
        [InlineData("Development", "D")]
        [InlineData("Test", "T")]
        [InlineData("Production", "P")]
        public void WithEnvironmentAwareTimestampErrorIds_ShouldSetEnvironmentAwareTimestampGenerator(string environment, string expectedPrefix)
        {
            // Arrange
            var options = new ExceptionHandlerOptions();

            // Act
            var result = options.WithEnvironmentAwareTimestampErrorIds(environment);

            // Assert
            result.Should().Be(options);
            options.ErrorIdGenerator.Should().NotBeNull();
            var generatedId = options.ErrorIdGenerator!();
            System.Text.RegularExpressions.Regex.IsMatch(generatedId, $"^{expectedPrefix}-\\d{{8}}-\\d{{6}}-\\d{{4}}$").Should().BeTrue();
        }

        [Theory]
        [InlineData("Development", "D")]
        [InlineData("Test", "T")]
        [InlineData("Production", "P")]
        public void WithEnvironmentAwareGuidErrorIds_ShouldSetEnvironmentAwareGuidGenerator(string environment, string expectedPrefix)
        {
            // Arrange
            var options = new ExceptionHandlerOptions();

            // Act
            var result = options.WithEnvironmentAwareGuidErrorIds(environment);

            // Assert
            result.Should().Be(options);
            options.ErrorIdGenerator.Should().NotBeNull();
            var generatedId = options.ErrorIdGenerator!();
            System.Text.RegularExpressions.Regex.IsMatch(generatedId, $"^{expectedPrefix}-[a-f0-9]{{8}}$").Should().BeTrue();
        }

        [Theory]
        [InlineData("Development", "D")]
        [InlineData("Test", "T")]
        [InlineData("Production", "P")]
        public void WithEnvironmentAwareSequentialErrorIds_ShouldSetEnvironmentAwareSequentialGenerator(string environment, string expectedPrefix)
        {
            // Arrange
            var options = new ExceptionHandlerOptions();

            // Act
            var result = options.WithEnvironmentAwareSequentialErrorIds(environment);

            // Assert
            result.Should().Be(options);
            options.ErrorIdGenerator.Should().NotBeNull();
            var generatedId = options.ErrorIdGenerator!();
            System.Text.RegularExpressions.Regex.IsMatch(generatedId, $"^{expectedPrefix}-\\d{{13}}$").Should().BeTrue();
        }

        [Fact]
        public void WithSharedPostProcessing_ShouldSetPostProcessingAction()
        {
            // Arrange
            var options = new ExceptionHandlerOptions();
            var postProcessingCalled = false;

            // Act
            var result = options.WithSharedPostProcessing((exception, response) =>
            {
                postProcessingCalled = true;
                response.Context = new Dictionary<string, object> { ["processed"] = true };
            });

            // Assert
            result.Should().Be(options);
            options.SharedPostProcessingAction.Should().NotBeNull();
            
            // Test the action
            var exception = new Exception("Test");
            var response = new ExceptionResponse();
            options.SharedPostProcessingAction!(exception, response);
            
            postProcessingCalled.Should().BeTrue();
            response.Context.Should().ContainKey("processed");
        }

        [Fact]
        public void WithSharedPostProcessing_ShouldBeChainable()
        {
            // Arrange
            var options = new ExceptionHandlerOptions();

            // Act
            var result = options
                .WithCustomErrorIdGenerator(() => "TEST-123")
                .WithSharedPostProcessing((exception, response) => { })
                .WithEnvironmentAwareErrorIds("Development");

            // Assert
            result.Should().Be(options);
            options.ErrorIdGenerator.Should().NotBeNull();
            options.SharedPostProcessingAction.Should().NotBeNull();
        }

        // Test custom exception handlers for testing
        private class TestCustomExceptionHandler : ICustomExceptionHandler
        {
            public int Priority => 10;

            public bool CanHandle(Type exceptionType)
            {
                return exceptionType == typeof(ArgumentException);
            }

            public (int statusCode, string message) Handle(Exception exception, Dictionary<string, object>? context = null)
            {
                return (400, "Test handler");
            }
        }

        private class AnotherTestCustomExceptionHandler : ICustomExceptionHandler
        {
            public int Priority => 20;

            public bool CanHandle(Type exceptionType)
            {
                return exceptionType == typeof(InvalidOperationException);
            }

            public (int statusCode, string message) Handle(Exception exception, Dictionary<string, object>? context = null)
            {
                return (422, "Another test handler");
            }
        }
    }
} 