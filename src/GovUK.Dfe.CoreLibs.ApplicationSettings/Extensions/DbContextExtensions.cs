using GovUK.Dfe.CoreLibs.ApplicationSettings.Configuration;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Entities;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Extensions;

public static class DbContextExtensions
{
    /// <summary>
    /// Adds ApplicationSettings entity configuration to an existing DbContext
    /// </summary>
    /// <param name="modelBuilder">The ModelBuilder instance</param>
    /// <param name="schema">Optional schema name. If null, uses default schema</param>
    /// <param name="tableName">Optional table name. Defaults to "ApplicationSettings"</param>
    public static void ConfigureApplicationSettings(
        this ModelBuilder modelBuilder,
        string? schema = null,
        string tableName = "ApplicationSettings")
    {
        // Apply default schema if specified
        if (!string.IsNullOrEmpty(schema))
        {
            modelBuilder.HasDefaultSchema(schema);
        }

        modelBuilder.Entity<ApplicationSetting>(entity =>
        {
            // Configure table with optional schema
            if (!string.IsNullOrEmpty(schema))
            {
                entity.ToTable(tableName, schema);
            }
            else
            {
                entity.ToTable(tableName);
            }

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Key)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Value)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("General");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(255);

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(255);

            // Create unique index on Key for fast lookups
            entity.HasIndex(e => e.Key)
                .IsUnique()
                .HasDatabaseName("IX_ApplicationSettings_Key");

            // Create index on Category for filtered queries
            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_ApplicationSettings_Category");
        });
    }

    /// <summary>
    /// Adds ApplicationSettings entity configuration using options from DI
    /// </summary>
    /// <param name="modelBuilder">The ModelBuilder instance</param>
    /// <param name="options">ApplicationSettings options</param>
    public static void ConfigureApplicationSettings(
        this ModelBuilder modelBuilder,
        ApplicationSettingsOptions options)
    {
        modelBuilder.ConfigureApplicationSettings(options.Schema, "ApplicationSettings");
    }
}