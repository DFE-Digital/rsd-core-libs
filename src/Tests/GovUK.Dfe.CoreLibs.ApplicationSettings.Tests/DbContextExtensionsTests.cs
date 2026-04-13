using FluentAssertions;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Configuration;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Entities;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Extensions;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Tests.Extensions;

public class DbContextExtensionsTests
{
    [Fact]
    public void ConfigureApplicationSettings_WithDefaultParameters_ShouldConfigureEntity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act & Assert - Should not throw
        using var context = new TestDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(ApplicationSetting));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("ApplicationSettings");
    }

    [Fact]
    public void ConfigureApplicationSettings_WithCustomSchemaAndTable_ShouldApplyConfiguration()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContextWithSchema>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act & Assert - Should not throw
        using var context = new TestDbContextWithSchema(options);
        var entityType = context.Model.FindEntityType(typeof(ApplicationSetting));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("CustomTable");
    }

    [Fact]
    public void ConfigureApplicationSettings_WithOptions_ShouldConfigureCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContextWithOptions>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act & Assert - Should not throw
        using var context = new TestDbContextWithOptions(options);
        var entityType = context.Model.FindEntityType(typeof(ApplicationSetting));

        entityType.Should().NotBeNull();
    }

    // Test DbContext classes
    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<ApplicationSetting> ApplicationSettings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureApplicationSettings();
            base.OnModelCreating(modelBuilder);
        }
    }

    private class TestDbContextWithSchema : DbContext
    {
        public TestDbContextWithSchema(DbContextOptions<TestDbContextWithSchema> options) : base(options) { }
        public DbSet<ApplicationSetting> ApplicationSettings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureApplicationSettings("CustomSchema", "CustomTable");
            base.OnModelCreating(modelBuilder);
        }
    }

    private class TestDbContextWithOptions : DbContext
    {
        public TestDbContextWithOptions(DbContextOptions<TestDbContextWithOptions> options) : base(options) { }
        public DbSet<ApplicationSetting> ApplicationSettings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var appOptions = new ApplicationSettingsOptions { Schema = "TestSchema" };
            modelBuilder.ConfigureApplicationSettings(appOptions);
            base.OnModelCreating(modelBuilder);
        }
    }
}