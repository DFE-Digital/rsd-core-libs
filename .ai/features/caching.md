# Feature: Caching

`AddServiceCaching` / `AddRedisCaching` / `AddHybridCaching`. `ICacheService<T>.GetOrAddAsync` durations keyed by **handler method name** in `CacheSettings`.

FlexForms API prefixes keys with `t:{tenantId}:` in the **consumer**, not inside this package. Don’t assume CoreLibs is tenant-aware.

Session: `SessionDistributedCacheAdapter.PropagateReadErrors => true`.
