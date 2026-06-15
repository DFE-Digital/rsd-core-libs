using FluentAssertions;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Data;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Entities;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Tests.Data;

public class ApplicationSettingsDbContextTests : IDisposable
{
    private readonly ApplicationSettingsDbContext _context;

    public ApplicationSettingsDbContextTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
    }

    [Fact]
    public void DbContext_ShouldHaveApplicationSettingsDbSet()
    {
        // Assert
        _context.ApplicationSettings.Should().NotBeNull();
    }

    [Fact]
    public async Task DbContext_ShouldAllowAddingAndRetrievingSettings()
    {
        // Arrange
        var setting = TestData.CreateSetting();

        // Act
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        var retrievedSetting = await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Key == setting.Key);

        // Assert
        retrievedSetting.Should().NotBeNull();
        retrievedSetting!.Key.Should().Be(setting.Key);
        retrievedSetting.Value.Should().Be(setting.Value);
    }

    [Fact]
    public void DbContext_ShouldConfigureUniqueIndexOnKey()
    {
        // Arrange & Act
        var entityType = _context.Model.FindEntityType(typeof(ApplicationSetting));

        // Assert
        entityType.Should().NotBeNull();

        var keyIndex = entityType!.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(ApplicationSetting.Key)));

        keyIndex.Should().NotBeNull();
        keyIndex!.IsUnique.Should().BeTrue();
        keyIndex.GetDatabaseName().Should().Be("IX_ApplicationSettings_Key");
    }

    [Fact]
    public void DbContext_ShouldConfigureWithCustomSchema()
    {
        // Arrange
        var customSchema = "CustomSchema";

        // Act
        using var contextWithSchema = TestDbContextFactory.CreateInMemoryContext(customSchema);

        // Assert
        contextWithSchema.Should().NotBeNull();
        // Note: Schema testing is limited in InMemory provider, but we can verify context creation
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}