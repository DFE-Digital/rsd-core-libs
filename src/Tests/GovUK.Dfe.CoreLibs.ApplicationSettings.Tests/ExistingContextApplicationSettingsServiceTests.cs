using FluentAssertions;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Configuration;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Entities;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Services;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Tests.Services;

public class ExistingContextApplicationSettingsServiceTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly MemoryCache _cache;
    private readonly Mock<ILogger<ExistingContextApplicationSettingsService<TestDbContext>>> _mockLogger;
    private readonly ApplicationSettingsOptions _options;
    private readonly ExistingContextApplicationSettingsService<TestDbContext> _service;

    public ExistingContextApplicationSettingsServiceTests()
    {
        _context = CreateTestDbContext();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<ExistingContextApplicationSettingsService<TestDbContext>>>();
        _options = new ApplicationSettingsOptions
        {
            EnableCaching = true,
            CacheExpirationMinutes = 30,
            DefaultCategory = "General"
        };

        _service = new ExistingContextApplicationSettingsService<TestDbContext>(
            _context,
            _cache,
            Options.Create(_options),
            _mockLogger.Object);
    }

    #region GetSettingAsync Tests

    [Fact]
    public async Task GetSettingAsync_WithValidKey_ShouldReturnValue()
    {
        // Arrange
        var setting = TestData.CreateSetting("TestKey", "TestValue");
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSettingAsync("TestKey");

        // Assert
        result.Should().Be("TestValue");
    }

    [Fact]
    public async Task GetSettingAsync_WithInvalidKey_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetSettingAsync("NonExistentKey");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSettingAsync_WithNullOrEmptyKey_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act1 = async () => await _service.GetSettingAsync(null!);
        var act2 = async () => await _service.GetSettingAsync("");
        var act3 = async () => await _service.GetSettingAsync("   ");

        await act1.Should().ThrowAsync<ArgumentException>().WithMessage("*Setting key cannot be null or empty*");
        await act2.Should().ThrowAsync<ArgumentException>().WithMessage("*Setting key cannot be null or empty*");
        await act3.Should().ThrowAsync<ArgumentException>().WithMessage("*Setting key cannot be null or empty*");
    }

    [Fact]
    public async Task GetSettingAsync_WithInactiveSetting_ShouldReturnNull()
    {
        // Arrange
        var setting = TestData.CreateSetting("InactiveKey", "InactiveValue", isActive: false);
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSettingAsync("InactiveKey");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSettingAsync_WithCachingEnabled_ShouldReturnCachedValue()
    {
        // Arrange
        var setting = TestData.CreateSetting("CachedKey", "CachedValue");
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // First call to cache the value
        await _service.GetSettingAsync("CachedKey");

        // Remove from database
        _context.ApplicationSettings.Remove(setting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSettingAsync("CachedKey");

        // Assert
        result.Should().Be("CachedValue");
    }

    [Fact]
    public async Task GetSettingAsync_WithCachingDisabled_ShouldNotUseCache()
    {
        // Arrange
        _options.EnableCaching = false;
        var service = new ExistingContextApplicationSettingsService<TestDbContext>(
            _context, _cache, Options.Create(_options), _mockLogger.Object);

        var setting = TestData.CreateSetting("NoCacheKey", "NoCacheValue");
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // First call
        await service.GetSettingAsync("NoCacheKey");

        // Remove from database
        _context.ApplicationSettings.Remove(setting);
        await _context.SaveChangesAsync();

        // Act
        var result = await service.GetSettingAsync("NoCacheKey");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetSettingAsync Generic Tests

    [Fact]
    public async Task GetSettingAsync_Generic_WithValidJson_ShouldReturnDeserialized()
    {
        // Arrange
        var testObject = new TestModel { Name = "Test", Value = 123 };
        var jsonValue = JsonSerializer.Serialize(testObject);
        var setting = TestData.CreateSetting("JsonKey", jsonValue);
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSettingAsync<TestModel>("JsonKey");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(123);
    }

    [Fact]
    public async Task GetSettingAsync_Generic_WithInvalidJson_ShouldReturnNullAndLogError()
    {
        // Arrange
        var setting = TestData.CreateSetting("InvalidJsonKey", "{ invalid json");
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSettingAsync<TestModel>("InvalidJsonKey");

        // Assert
        result.Should().BeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to deserialize setting InvalidJsonKey")),
                It.IsAny<JsonException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSettingAsync_Generic_WithNonExistentKey_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetSettingAsync<TestModel>("NonExistentKey");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetSettingAsync With Default Value Tests

    [Fact]
    public async Task GetSettingAsync_WithDefaultValue_WhenSettingExists_ShouldReturnActualValue()
    {
        // Arrange
        var setting = TestData.CreateSetting("ExistingKey", "ActualValue");
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSettingAsync("ExistingKey", "DefaultValue");

        // Assert
        result.Should().Be("ActualValue");
    }

    [Fact]
    public async Task GetSettingAsync_WithDefaultValue_WhenSettingDoesNotExist_ShouldReturnDefaultValue()
    {
        // Act
        var result = await _service.GetSettingAsync("NonExistentKey", "DefaultValue");

        // Assert
        result.Should().Be("DefaultValue");
    }

    #endregion

    #region GetSettingsByCategoryAsync Tests

    [Fact]
    public async Task GetSettingsByCategoryAsync_WithValidCategory_ShouldReturnCategorySettings()
    {
        // Arrange
        var settings = new[]
        {
            TestData.CreateSetting("Key1", "Value1", "General"),
            TestData.CreateSetting("Key2", "Value2", "Security"),
            TestData.CreateSetting("Key3", "Value3", "General"),
            TestData.CreateSetting("Key4", "Value4", "General", isActive: false)
        };
        _context.ApplicationSettings.AddRange(settings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSettingsByCategoryAsync("General");

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKeys("Key1", "Key3");
        result["Key1"].Should().Be("Value1");
        result["Key3"].Should().Be("Value3");
    }

    [Fact]
    public async Task GetSettingsByCategoryAsync_WithNonExistentCategory_ShouldReturnEmptyDictionary()
    {
        // Act
        var result = await _service.GetSettingsByCategoryAsync("NonExistentCategory");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSettingsByCategoryAsync_WithNullOrEmptyCategory_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act1 = async () => await _service.GetSettingsByCategoryAsync(null!);
        var act2 = async () => await _service.GetSettingsByCategoryAsync("");
        var act3 = async () => await _service.GetSettingsByCategoryAsync("   ");

        await act1.Should().ThrowAsync<ArgumentException>().WithMessage("*Category cannot be null or empty*");
        await act2.Should().ThrowAsync<ArgumentException>().WithMessage("*Category cannot be null or empty*");
        await act3.Should().ThrowAsync<ArgumentException>().WithMessage("*Category cannot be null or empty*");
    }

    #endregion

    #region GetAllSettingsAsync Tests

    [Fact]
    public async Task GetAllSettingsAsync_ShouldReturnAllActiveSettings()
    {
        // Arrange
        var settings = new[]
        {
            TestData.CreateSetting("Key1", "Value1", "General"),
            TestData.CreateSetting("Key2", "Value2", "Security"),
            TestData.CreateSetting("Key3", "Value3", "General", isActive: false)
        };
        _context.ApplicationSettings.AddRange(settings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllSettingsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKeys("Key1", "Key2");
        result["Key1"].Should().Be("Value1");
        result["Key2"].Should().Be("Value2");
    }

    [Fact]
    public async Task GetAllSettingsAsync_WithNoSettings_ShouldReturnEmptyDictionary()
    {
        // Act
        var result = await _service.GetAllSettingsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region SetSettingAsync Tests

    [Fact]
    public async Task SetSettingAsync_WithNewSetting_ShouldCreateSetting()
    {
        // Act
        await _service.SetSettingAsync("NewKey", "NewValue", "New Description", "NewCategory", "TestUser");

        // Assert
        var setting = await _context.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == "NewKey");
        setting.Should().NotBeNull();
        setting!.Value.Should().Be("NewValue");
        setting.Description.Should().Be("New Description");
        setting.Category.Should().Be("NewCategory");
        setting.CreatedBy.Should().Be("TestUser");
        setting.UpdatedBy.Should().Be("TestUser");
        setting.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SetSettingAsync_WithExistingSetting_ShouldUpdateSetting()
    {
        // Arrange
        var existingSetting = TestData.CreateSetting("ExistingKey", "OldValue", "OldCategory", "Old Description");
        _context.ApplicationSettings.Add(existingSetting);
        await _context.SaveChangesAsync();

        // Act
        await _service.SetSettingAsync("ExistingKey", "NewValue", "New Description", "NewCategory", "TestUser");

        // Assert
        var setting = await _context.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == "ExistingKey");
        setting.Should().NotBeNull();
        setting!.Value.Should().Be("NewValue");
        setting.Description.Should().Be("New Description");
        setting.Category.Should().Be("NewCategory");
        setting.UpdatedBy.Should().Be("TestUser");
        setting.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SetSettingAsync_WithNullOrEmptyKey_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act1 = async () => await _service.SetSettingAsync(null!, "Value");
        var act2 = async () => await _service.SetSettingAsync("", "Value");
        var act3 = async () => await _service.SetSettingAsync("   ", "Value");

        await act1.Should().ThrowAsync<ArgumentException>().WithMessage("*Setting key cannot be null or empty*");
        await act2.Should().ThrowAsync<ArgumentException>().WithMessage("*Setting key cannot be null or empty*");
        await act3.Should().ThrowAsync<ArgumentException>().WithMessage("*Setting key cannot be null or empty*");
    }

    [Fact]
    public async Task SetSettingAsync_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _service.SetSettingAsync("Key", null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("value");
    }

    [Fact]
    public async Task SetSettingAsync_ShouldInvalidateCache()
    {
        // Arrange
        var setting = TestData.CreateSetting("CacheKey", "OldValue");
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Cache the value
        await _service.GetSettingAsync("CacheKey");

        // Act
        await _service.SetSettingAsync("CacheKey", "NewValue");

        // Assert
        var result = await _service.GetSettingAsync("CacheKey");
        result.Should().Be("NewValue");
    }

    [Fact]
    public async Task SetSettingAsync_ShouldLogInformation()
    {
        // Act
        await _service.SetSettingAsync("TestKey", "TestValue", updatedBy: "TestUser");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Setting TestKey updated by TestUser")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region SetSettingsAsync Tests

    [Fact]
    public async Task SetSettingsAsync_WithMultipleSettings_ShouldSetAllSettings()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            { "Key1", "Value1" },
            { "Key2", "Value2" },
            { "Key3", "Value3" }
        };

        // Act
        await _service.SetSettingsAsync(settings, "TestCategory", "TestUser");

        // Assert
        var savedSettings = await _context.ApplicationSettings.ToListAsync();
        savedSettings.Should().HaveCount(3);
        savedSettings.All(s => s.Category == "TestCategory").Should().BeTrue();
        savedSettings.All(s => s.CreatedBy == "TestUser").Should().BeTrue();
    }

    [Fact]
    public async Task SetSettingsAsync_WithNullOrEmptyDictionary_ShouldDoNothing()
    {
        // Act
        await _service.SetSettingsAsync(null!);
        await _service.SetSettingsAsync(new Dictionary<string, string>());

        // Assert
        var settings = await _context.ApplicationSettings.ToListAsync();
        settings.Should().BeEmpty();
    }

    #endregion

    #region DeleteSettingAsync Tests

    [Fact]
    public async Task DeleteSettingAsync_WithExistingSetting_ShouldSoftDeleteSetting()
    {
        // Arrange
        var setting = TestData.CreateSetting("DeleteKey", "DeleteValue");
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteSettingAsync("DeleteKey");

        // Assert
        var deletedSetting = await _context.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == "DeleteKey");
        deletedSetting.Should().NotBeNull();
        deletedSetting!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSettingAsync_WithNonExistentSetting_ShouldDoNothing()
    {
        // Arrange
        var initialCount = await _context.ApplicationSettings.CountAsync();

        // Act
        await _service.DeleteSettingAsync("NonExistentKey");

        // Assert
        var finalCount = await _context.ApplicationSettings.CountAsync();
        finalCount.Should().Be(initialCount, "database should remain unchanged");

        // Verify no delete logging occurred
        _mockLogger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("deleted")),
                It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteSettingAsync_WithNullOrEmptyKey_ShouldDoNothing()
    {
        // Arrange
        var existingSetting = TestData.CreateSetting("TestKey", "TestValue");
        _context.ApplicationSettings.Add(existingSetting);
        await _context.SaveChangesAsync();

        var initialCount = await _context.ApplicationSettings.CountAsync();

        // Act - Test all invalid key scenarios
        await _service.DeleteSettingAsync(null!);
        await _service.DeleteSettingAsync("");
        await _service.DeleteSettingAsync("   ");

        // Assert
        var finalCount = await _context.ApplicationSettings.CountAsync();
        finalCount.Should().Be(initialCount, "database should remain unchanged for invalid keys");

        // Verify existing setting is unchanged
        var setting = await _context.ApplicationSettings.FirstAsync(s => s.Key == "TestKey");
        setting.IsActive.Should().BeTrue("existing setting should remain active");

        // Verify no delete logging occurred
        _mockLogger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("deleted")),
                It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteSettingAsync_ShouldInvalidateCache()
    {
        // Arrange
        var setting = TestData.CreateSetting("CacheDeleteKey", "CacheDeleteValue");
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Cache the value
        await _service.GetSettingAsync("CacheDeleteKey");

        // Act
        await _service.DeleteSettingAsync("CacheDeleteKey");

        // Assert
        var result = await _service.GetSettingAsync("CacheDeleteKey");
        result.Should().BeNull();
    }

    #endregion

    #region SettingExistsAsync Tests

    [Fact]
    public async Task SettingExistsAsync_WithExistingActiveSetting_ShouldReturnTrue()
    {
        // Arrange
        var setting = TestData.CreateSetting("ExistsKey", "ExistsValue");
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SettingExistsAsync("ExistsKey");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SettingExistsAsync_WithExistingInactiveSetting_ShouldReturnFalse()
    {
        // Arrange
        var setting = TestData.CreateSetting("InactiveExistsKey", "InactiveExistsValue", isActive: false);
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SettingExistsAsync("InactiveExistsKey");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SettingExistsAsync_WithNonExistentSetting_ShouldReturnFalse()
    {
        // Act
        var result = await _service.SettingExistsAsync("NonExistentKey");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SettingExistsAsync_WithNullOrEmptyKey_ShouldReturnFalse()
    {
        // Act & Assert
        var result1 = await _service.SettingExistsAsync(null!);
        var result2 = await _service.SettingExistsAsync("");
        var result3 = await _service.SettingExistsAsync("   ");

        result1.Should().BeFalse();
        result2.Should().BeFalse();
        result3.Should().BeFalse();
    }

    #endregion

    #region RefreshCacheAsync Tests

    [Fact]
    public async Task RefreshCacheAsync_WithCachingEnabled_ShouldCacheAllActiveSettings()
    {
        // Arrange
        var settings = new[]
        {
            TestData.CreateSetting("Key1", "Value1"),
            TestData.CreateSetting("Key2", "Value2"),
            TestData.CreateSetting("Key3", "Value3", isActive: false)
        };
        _context.ApplicationSettings.AddRange(settings);
        await _context.SaveChangesAsync();

        // Act
        await _service.RefreshCacheAsync();

        // Remove settings from database to test cache
        _context.ApplicationSettings.RemoveRange(settings);
        await _context.SaveChangesAsync();

        // Assert - should still get values from cache
        var result1 = await _service.GetSettingAsync("Key1");
        var result2 = await _service.GetSettingAsync("Key2");
        var result3 = await _service.GetSettingAsync("Key3");

        result1.Should().Be("Value1");
        result2.Should().Be("Value2");
        result3.Should().BeNull(); // Inactive setting should not be cached
    }

    [Fact]
    public async Task RefreshCacheAsync_WithCachingDisabled_ShouldDoNothing()
    {
        // Arrange
        _options.EnableCaching = false;
        var service = new ExistingContextApplicationSettingsService<TestDbContext>(
            _context, _cache, Options.Create(_options), _mockLogger.Object);

        var setting = TestData.CreateSetting("CacheDisabledKey", "CacheDisabledValue");
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Act
        await service.RefreshCacheAsync();

        // Remove setting from database
        _context.ApplicationSettings.Remove(setting);
        await _context.SaveChangesAsync();

        // Assert
        var result = await service.GetSettingAsync("CacheDisabledKey");
        result.Should().BeNull(); // Should not be cached
    }

    #endregion

    #region Helper Classes and Methods

    private static TestDbContext CreateTestDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}

// Test DbContext for testing ExistingContextApplicationSettingsService
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique().HasDatabaseName("IX_ApplicationSettings_Key");
            entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
        });
    }
}

// Test model for JSON deserialization tests
public class TestModel
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}