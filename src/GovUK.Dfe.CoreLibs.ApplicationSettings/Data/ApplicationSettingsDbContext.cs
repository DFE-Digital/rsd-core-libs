using GovUK.Dfe.CoreLibs.ApplicationSettings.Configuration;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Entities;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Data;

public class ApplicationSettingsDbContext : DbContext
{
    private readonly ApplicationSettingsOptions _options;

    public ApplicationSettingsDbContext(DbContextOptions<ApplicationSettingsDbContext> options, IOptions<ApplicationSettingsOptions> settingsOptions)
        : base(options)
    {
        _options = settingsOptions.Value;
    }

    public DbSet<ApplicationSetting> ApplicationSettings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Use the extension method for configuration
        modelBuilder.ConfigureApplicationSettings(_options);

        base.OnModelCreating(modelBuilder);
    }
}