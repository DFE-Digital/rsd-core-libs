using FluentAssertions;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Configuration;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Data;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Extensions;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Interfaces;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configuration;

    public ServiceCollectionExtensionsTests()
    {
        _services = new ServiceCollection();
        _services.AddLogging();
        _configuration = CreateTestConfiguration();
    }

    #region AddApplicationSettings with IConfiguration Tests

    [Fact]
    public void AddApplicationSettings_WithConfiguration_ShouldRegisterAllServices()
    {
        // Act
        _services.AddApplicationSettings(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IApplicationSettingsService>().Should().NotBeNull();
        serviceProvider.GetService<ApplicationSettingsDbContext>().Should().NotBeNull();
        serviceProvider.GetService<IMemoryCache>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<ApplicationSettingsOptions>>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationSettings_WithConfiguration_ShouldRegisterCorrectImplementation()
    {
        // Act
        _services.AddApplicationSettings(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IApplicationSettingsService>();
        service.Should().BeOfType<ApplicationSettingsService>();
    }

    [Fact]
    public void AddApplicationSettings_WithConfigurationAndCustomConnectionString_ShouldUseCustomConnectionString()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:CustomConnection", "Server=custom;Database=CustomDb;" }
            })
            .Build();

        // Act
        _services.AddApplicationSettings(config, "CustomConnection");
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var dbContext = serviceProvider.GetService<ApplicationSettingsDbContext>();
        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationSettings_WithConfigurationAndSchema_ShouldSetSchema()
    {
        // Act
        _services.AddApplicationSettings(_configuration, schema: "CustomSchema");
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<ApplicationSettingsOptions>>()?.Value;
        options.Should().NotBeNull();
        options!.Schema.Should().Be("CustomSchema");
    }

    [Fact]
    public void AddApplicationSettings_WithConfigurationSection_ShouldReadFromConfiguration()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=test;Database=TestDb;" },
                { "ApplicationSettings:EnableCaching", "false" },
                { "ApplicationSettings:CacheExpirationMinutes", "60" },
                { "ApplicationSettings:DefaultCategory", "Custom" },
                { "ApplicationSettings:EnableEncryption", "true" },
                { "ApplicationSettings:EncryptionKey", "TestKey123" },
                { "ApplicationSettings:Schema", "ConfigSchema" }
            })
            .Build();

        // Act
        _services.AddApplicationSettings(config);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<ApplicationSettingsOptions>>()?.Value;
        options.Should().NotBeNull();
        options!.EnableCaching.Should().BeFalse();
        options.CacheExpirationMinutes.Should().Be(60);
        options.DefaultCategory.Should().Be("Custom");
        options.Schema.Should().Be("ConfigSchema");
    }

    [Fact]
    public void AddApplicationSettings_WithSchemaParameterOverridingConfig_ShouldUseParameterSchema()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=test;Database=TestDb;" },
                { "ApplicationSettings:Schema", "ConfigSchema" }
            })
            .Build();

        // Act
        _services.AddApplicationSettings(config, schema: "ParameterSchema");
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<ApplicationSettingsOptions>>()?.Value;
        options.Should().NotBeNull();
        options!.Schema.Should().Be("ParameterSchema");
    }

    [Fact]
    public void AddApplicationSettings_WithInvalidConfigurationValues_ShouldUseDefaults()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=test;Database=TestDb;" },
                { "ApplicationSettings:EnableCaching", "invalid_bool" },
                { "ApplicationSettings:CacheExpirationMinutes", "invalid_int" },
                { "ApplicationSettings:EnableEncryption", "not_a_boolean" }
            })
            .Build();

        // Act
        _services.AddApplicationSettings(config);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<ApplicationSettingsOptions>>()?.Value;
        options.Should().NotBeNull();
        options!.EnableCaching.Should().BeTrue(); // Default value
        options.CacheExpirationMinutes.Should().Be(30); // Default value
        options.DefaultCategory.Should().Be("General"); // Default value
    }

    [Fact]
    public void AddApplicationSettings_ShouldReturnServiceCollection()
    {
        // Act
        var result = _services.AddApplicationSettings(_configuration);

        // Assert
        result.Should().BeSameAs(_services);
    }

    #endregion

    #region AddApplicationSettingsWithExistingContext Tests

    [Fact]
    public void AddApplicationSettingsWithExistingContext_ShouldRegisterCorrectImplementation()
    {
        // Arrange - Register the TestDbContext in the DI container
        _services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // Act
        _services.AddApplicationSettingsWithExistingContext<TestDbContext>(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IApplicationSettingsService>();
        service.Should().BeOfType<ExistingContextApplicationSettingsService<TestDbContext>>();
    }

    [Fact]
    public void AddApplicationSettingsWithExistingContext_ShouldNotRegisterDbContext()
    {
        // Arrange - Register the TestDbContext in the DI container
        _services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // Act
        _services.AddApplicationSettingsWithExistingContext<TestDbContext>(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert - Should not register ApplicationSettingsDbContext since we're using existing context
        var applicationSettingsDbContext = serviceProvider.GetService<ApplicationSettingsDbContext>();
        applicationSettingsDbContext.Should().BeNull();
    }

    [Fact]
    public void AddApplicationSettingsWithExistingContext_ShouldRegisterMemoryCache()
    {
        // Arrange - Register the TestDbContext in the DI container
        _services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // Act
        _services.AddApplicationSettingsWithExistingContext<TestDbContext>(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var memoryCache = serviceProvider.GetService<IMemoryCache>();
        memoryCache.Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationSettingsWithExistingContext_WithConfiguration_ShouldReadFromConfiguration()
    {
        // Arrange
        _services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ApplicationSettings:EnableCaching", "false" },
                { "ApplicationSettings:CacheExpirationMinutes", "45" },
                { "ApplicationSettings:DefaultCategory", "ExistingContext" },
                { "ApplicationSettings:EnableEncryption", "true" },
                { "ApplicationSettings:EncryptionKey", "ExistingKey123" }
            })
            .Build();

        // Act
        _services.AddApplicationSettingsWithExistingContext<TestDbContext>(config);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<ApplicationSettingsOptions>>()?.Value;
        options.Should().NotBeNull();
        options!.EnableCaching.Should().BeFalse();
        options.CacheExpirationMinutes.Should().Be(45);
        options.DefaultCategory.Should().Be("ExistingContext");
    }

    [Fact]
    public void AddApplicationSettingsWithExistingContext_WithSchema_ShouldSetSchema()
    {
        // Arrange - Register the TestDbContext in the DI container
        _services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // Act
        _services.AddApplicationSettingsWithExistingContext<TestDbContext>(_configuration, "ExistingSchema");
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<ApplicationSettingsOptions>>()?.Value;
        options.Should().NotBeNull();
        options!.Schema.Should().Be("ExistingSchema");
    }

    [Fact]
    public void AddApplicationSettingsWithExistingContext_ShouldReturnServiceCollection()
    {
        // Act
        var result = _services.AddApplicationSettingsWithExistingContext<TestDbContext>(_configuration);

        // Assert
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void AddApplicationSettingsWithExistingContext_ShouldRegisterServiceWithCorrectLifetime()
    {
        // Arrange - Register the TestDbContext in the DI container
        _services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // Act
        _services.AddApplicationSettingsWithExistingContext<TestDbContext>(_configuration);

        // Assert
        var serviceDescriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(IApplicationSettingsService));
        serviceDescriptor.Should().NotBeNull();
        serviceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        serviceDescriptor.ImplementationType.Should().Be(typeof(ExistingContextApplicationSettingsService<TestDbContext>));
    }

    #endregion

    #region AddApplicationSettings with ConnectionString Tests

    [Fact]
    public void AddApplicationSettings_WithConnectionString_ShouldRegisterAllServices()
    {
        // Arrange
        const string connectionString = "Server=test;Database=TestDb;";

        // Act
        _services.AddApplicationSettings(connectionString);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IApplicationSettingsService>().Should().NotBeNull();
        serviceProvider.GetService<ApplicationSettingsDbContext>().Should().NotBeNull();
        serviceProvider.GetService<IMemoryCache>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<ApplicationSettingsOptions>>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationSettings_WithConnectionString_ShouldUseDefaultOptions()
    {
        // Arrange
        const string connectionString = "Server=test;Database=TestDb;";

        // Act
        _services.AddApplicationSettings(connectionString);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<ApplicationSettingsOptions>>()?.Value;
        options.Should().NotBeNull();
        options!.EnableCaching.Should().BeTrue();
        options.CacheExpirationMinutes.Should().Be(30);
        options.DefaultCategory.Should().Be("General");
        options.Schema.Should().BeNull();
    }

    [Fact]
    public void AddApplicationSettings_WithConnectionStringAndConfigureOptions_ShouldApplyCustomConfiguration()
    {
        // Arrange
        const string connectionString = "Server=test;Database=TestDb;";

        // Act
        _services.AddApplicationSettings(connectionString, options =>
        {
            options.EnableCaching = false;
            options.CacheExpirationMinutes = 120;
            options.DefaultCategory = "CustomCategory";
            options.Schema = "CustomSchema";
        });
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<ApplicationSettingsOptions>>()?.Value;
        options.Should().NotBeNull();
        options!.EnableCaching.Should().BeFalse();
        options.CacheExpirationMinutes.Should().Be(120);
        options.DefaultCategory.Should().Be("CustomCategory");
        options.Schema.Should().Be("CustomSchema");
    }

    [Fact]
    public void AddApplicationSettings_WithConnectionStringAndNullConfigureOptions_ShouldUseDefaults()
    {
        // Arrange
        const string connectionString = "Server=test;Database=TestDb;";

        // Act
        _services.AddApplicationSettings(connectionString, null);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<ApplicationSettingsOptions>>()?.Value;
        options.Should().NotBeNull();
        options!.EnableCaching.Should().BeTrue();
        options.CacheExpirationMinutes.Should().Be(30);
        options.DefaultCategory.Should().Be("General");
        options.Schema.Should().BeNull();
    }

    [Fact]
    public void AddApplicationSettings_WithConnectionString_ShouldReturnServiceCollection()
    {
        // Arrange
        const string connectionString = "Server=test;Database=TestDb;";

        // Act
        var result = _services.AddApplicationSettings(connectionString);

        // Assert
        result.Should().BeSameAs(_services);
    }

    #endregion

    #region Service Registration Tests

    [Fact]
    public void AddApplicationSettings_ShouldRegisterServicesWithCorrectLifetime()
    {
        // Act
        _services.AddApplicationSettings(_configuration);

        // Assert
        var serviceDescriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(IApplicationSettingsService));
        serviceDescriptor.Should().NotBeNull();
        serviceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);

        var dbContextDescriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(ApplicationSettingsDbContext));
        dbContextDescriptor.Should().NotBeNull();
        dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplicationSettings_CalledMultipleTimes_ShouldNotDuplicateMemoryCache()
    {
        // Act
        _services.AddApplicationSettings(_configuration);
        _services.AddApplicationSettings(_configuration);

        // Assert
        var memoryCacheDescriptors = _services.Where(s => s.ServiceType == typeof(IMemoryCache)).ToList();
        memoryCacheDescriptors.Should().HaveCount(1, "MemoryCache should only be registered once");
    }

    #endregion

    #region Helper Methods and Classes

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=test;Database=TestDb;" }
            })
            .Build();
    }

    #endregion
}

// Test DbContext for testing existing context scenarios
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Add any necessary model configuration here
    }
}