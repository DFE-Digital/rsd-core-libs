using GovUK.Dfe.CoreLibs.ApplicationSettings.Configuration;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Services;

public class ExistingContextApplicationSettingsService<TContext> : BaseApplicationSettingsService
    where TContext : DbContext
{
    private readonly TContext _context;

    public ExistingContextApplicationSettingsService(
        TContext context,
        IMemoryCache cache,
        IOptions<ApplicationSettingsOptions> options,
        ILogger<ExistingContextApplicationSettingsService<TContext>> logger)
        : base(cache, options, logger)
    {
        _context = context;
    }

    protected override DbSet<ApplicationSetting> GetApplicationSettingsDbSet()
    {
        return _context.Set<ApplicationSetting>();
    }

    protected override async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}