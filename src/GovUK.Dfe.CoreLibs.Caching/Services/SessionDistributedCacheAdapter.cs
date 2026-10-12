using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.CoreLibs.Caching.Services
{
    /// <summary>
    /// <see cref="DistributedCacheAdapter"/> for ASP.NET Core session storage, where read errors are rethrown
    /// instead of being reported as a cache miss.
    /// </summary>
    /// <remarks>
    /// ASP.NET Core treats a null read as an empty session and writes that empty session back at the end of
    /// the request, wiping the user's real session data. When the read throws, the session is marked
    /// unavailable for that request and is not written back.
    /// Wire it up in a web app with:
    /// <code>
    /// services.AddSingleton&lt;ISessionStore&gt;(sp => new DistributedSessionStore(
    ///     sp.GetRequiredService&lt;SessionDistributedCacheAdapter&gt;(),
    ///     sp.GetRequiredService&lt;ILoggerFactory&gt;()));
    /// </code>
    /// </remarks>
    public class SessionDistributedCacheAdapter(
        IAdvancedRedisCacheService cacheService,
        ILogger<DistributedCacheAdapter> logger)
        : DistributedCacheAdapter(cacheService, logger)
    {
        protected override bool PropagateReadErrors => true;
    }
}
