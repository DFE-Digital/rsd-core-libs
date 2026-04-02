using FluentAssertions;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Configuration;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Data;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Services;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Tests.Services;

public class ApplicationSettingsServiceTests : IDisposable
{
    private readonly ApplicationSettingsDbContext _context;
    private readonly MemoryCache _cache;
    private readonly Mock<ILogger<ApplicationSettingsService>> _mockLogger;
    private readonly ApplicationSettingsOptions _options;
    private readonly ApplicationSettingsService _service;

    public ApplicationSettingsServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<ApplicationSettingsService>>();
        _options = new ApplicationSettingsOptions();

        _service = new ApplicationSettingsService(
            _context,
            _cache,
            Options.Create(_options),
            _mockLogger.Object);
    }

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

        await act1.Should().ThrowAsync<ArgumentException>();
        await act2.Should().ThrowAsync<ArgumentException>();
        await act3.Should().ThrowAsync<ArgumentException>();
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
    public async Task GetSettingAsync_Generic_WithValidJson_ShouldReturnDeserialized()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 123 };
        var jsonValue = JsonSerializer.Serialize(testObject);
        var setting = TestData.CreateSetting("JsonKey", jsonValue);
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Act - Use object instead of dynamic
        var result = await _service.GetSettingAsync<object>("JsonKey");

        // Assert
        result.Should().NotBeNull();
        // You can also cast to JsonElement to access properties
        if (result != null) // Consider adding this defensive check
        {
            var jsonElement = (JsonElement)result;
            jsonElement.GetProperty("Name").GetString().Should().Be("Test");
            jsonElement.GetProperty("Value").GetInt32().Should().Be(123);
        }
    }

    [Fact]
    public async Task GetSettingAsync_WithDefaultValue_ShouldReturnDefaultWhenNotFound()
    {
        // Act
        var result = await _service.GetSettingAsync("NonExistentKey", "DefaultValue");

        // Assert
        result.Should().Be("DefaultValue");
    }

    [Fact]
    public async Task GetSettingsByCategoryAsync_ShouldReturnOnlyActiveSettingsInCategory()
    {
        // Arrange
        var settings = new[]
        {
            TestData.CreateSetting("Key1", "Value1", "TestCategory"),
            TestData.CreateSetting("Key2", "Value2", "TestCategory"),
            TestData.CreateSetting("Key3", "Value3", "OtherCategory"),
            TestData.CreateSetting("Key4", "Value4", "TestCategory", isActive: false)
        };

        _context.ApplicationSettings.AddRange(settings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSettingsByCategoryAsync("TestCategory");

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKeys("Key1", "Key2");
        result.Should().NotContainKeys("Key3", "Key4");
    }

    [Fact]
    public async Task GetAllSettingsAsync_ShouldReturnOnlyActiveSettings()
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
        var result = await _service.GetAllSettingsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKeys("Key1", "Key2");
        result.Should().NotContainKey("Key3");
    }

    [Fact]
    public async Task SetSettingAsync_WithNewSetting_ShouldCreateSetting()
    {
        // Act
        await _service.SetSettingAsync("NewKey", "NewValue", "Description", "Category", "User");

        // Assert
        var setting = await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Key == "NewKey");

        setting.Should().NotBeNull();
        setting!.Value.Should().Be("NewValue");
        setting.Description.Should().Be("Description");
        setting.Category.Should().Be("Category");
        setting.CreatedBy.Should().Be("User");
        setting.UpdatedBy.Should().Be("User");
    }

    [Fact]
    public async Task SetSettingAsync_WithExistingSetting_ShouldUpdateSetting()
    {
        // Arrange
        var existingSetting = TestData.CreateSetting("ExistingKey", "OldValue");
        _context.ApplicationSettings.Add(existingSetting);
        await _context.SaveChangesAsync();

        // Act
        await _service.SetSettingAsync("ExistingKey", "NewValue", "Updated Description", "Updated Category", "UpdateUser");

        // Assert
        var setting = await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Key == "ExistingKey");

        setting.Should().NotBeNull();
        setting!.Value.Should().Be("NewValue");
        setting.Description.Should().Be("Updated Description");
        setting.Category.Should().Be("Updated Category");
        setting.UpdatedBy.Should().Be("UpdateUser");
    }

    [Fact]
    public async Task SetSettingAsync_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _service.SetSettingAsync("TestKey", null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SetSettingsAsync_WithMultipleSettings_ShouldCreateAll()
    {
        // Arrange
        var settings = new Dictionary<string, string>
        {
            { "Key1", "Value1" },
            { "Key2", "Value2" },
            { "Key3", "Value3" }
        };

        // Act
        await _service.SetSettingsAsync(settings, "BatchCategory", "BatchUser");

        // Assert
        var savedSettings = await _context.ApplicationSettings.ToListAsync();
        savedSettings.Should().HaveCount(3);
        savedSettings.Should().AllSatisfy(s => s.Category.Should().Be("BatchCategory"));
    }

    [Fact]
    public async Task DeleteSettingAsync_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var setting = TestData.CreateSetting("DeleteKey", "DeleteValue");
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteSettingAsync("DeleteKey");

        // Assert
        var deletedSetting = await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Key == "DeleteKey");

        deletedSetting.Should().NotBeNull();
        deletedSetting!.IsActive.Should().BeFalse();
    }

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
    public async Task SettingExistsAsync_WithInactiveSetting_ShouldReturnFalse()
    {
        // Arrange
        var setting = TestData.CreateSetting("InactiveKey", "InactiveValue", isActive: false);
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SettingExistsAsync("InactiveKey");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SettingExistsAsync_WithNonExistentKey_ShouldReturnFalse()
    {
        // Act
        var result = await _service.SettingExistsAsync("NonExistentKey");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshCacheAsync_ShouldCacheAllActiveSettings()
    {
        // Arrange
        var settings = TestData.CreateMultipleSettings();
        _context.ApplicationSettings.AddRange(settings);
        await _context.SaveChangesAsync();

        // Act
        await _service.RefreshCacheAsync();

        // Assert - We can't directly verify cache contents, but we can verify no exceptions
        // and that subsequent calls use cache
        var result = await _service.GetSettingAsync("Setting1");
        result.Should().Be("Value1");
    }

    [Fact]
    public async Task GetSettingAsync_WithCachingEnabled_ShouldUseCacheOnSubsequentCalls()
    {
        // Arrange
        var setting = TestData.CreateSetting("CachedKey", "CachedValue");
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Act - First call should hit database
        var result1 = await _service.GetSettingAsync("CachedKey");

        // Modify database value
        setting.Value = "ModifiedValue";
        await _context.SaveChangesAsync();

        // Second call should return cached value
        var result2 = await _service.GetSettingAsync("CachedKey");

        // Assert
        result1.Should().Be("CachedValue");
        result2.Should().Be("CachedValue"); // Should be cached, not "ModifiedValue"
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }
}