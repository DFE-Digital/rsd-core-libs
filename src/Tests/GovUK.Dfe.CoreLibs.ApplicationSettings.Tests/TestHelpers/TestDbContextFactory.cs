using GovUK.Dfe.CoreLibs.ApplicationSettings.Configuration;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Tests.TestHelpers;

public static class TestDbContextFactory
{
    public static ApplicationSettingsDbContext CreateInMemoryContext(string? schema = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationSettingsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var settingsOptions = Options.Create(new ApplicationSettingsOptions
        {
            Schema = schema,
            EnableCaching = true,
            CacheExpirationMinutes = 30,
            DefaultCategory = "General"
        });

        return new ApplicationSettingsDbContext(options, settingsOptions);
    }
}