using GovUK.Dfe.CoreLibs.ApplicationSettings.Configuration;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Data;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Services;

public class ApplicationSettingsService : BaseApplicationSettingsService
{
    private readonly ApplicationSettingsDbContext _context;

    public ApplicationSettingsService(
        ApplicationSettingsDbContext context,
        IMemoryCache cache,
        IOptions<ApplicationSettingsOptions> options,
        ILogger<ApplicationSettingsService> logger)
        : base(cache, options, logger)
    {
        _context = context;
    }

    protected override DbSet<ApplicationSetting> GetApplicationSettingsDbSet()
    {
        return _context.ApplicationSettings;
    }

    protected override async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}