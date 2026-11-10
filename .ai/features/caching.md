# Feature: Caching

`AddServiceCaching` / `AddRedisCaching` / `AddHybridCaching`. `ICacheService<T>.GetOrAddAsync` durations keyed by **handler method name** in `CacheSettings`.

FlexForms API prefixes keys with `t:{tenantId}:` in the **consumer**, not inside this package. Don’t assume CoreLibs is tenant-aware.

Session: `SessionDistributedCacheAdapter.PropagateReadErrors => true`.

`AddHybridCaching` is **not** a two-level cache: it registers an in-process `ICacheService<IMemoryCacheType>` and a Redis `ICacheService<IRedisCacheType>` side by side, with no read-through and no cross-replica invalidation. Memory-cache `Remove` affects one process only (**Verified**, `MemoryCacheService`). FlexForms Web therefore builds its own two-level cache for templates and status labels (`TwoLevelCache`: short memory copy, then `IAdvancedRedisCacheService`, with Redis pub/sub to drop memory copies on every replica), and keeps tenant configuration memory-only with the same pub/sub invalidation; see flexforms-web `.ai/known-pitfalls.md` items 23–24.

`RedisCacheService.GetOrAddAsync` serialises with camelCase System.Text.Json. A `RedisException` falls back to the fetch function and a `JsonException` deletes the entry and refetches (**Verified**), so cached types must round-trip through that serializer.
