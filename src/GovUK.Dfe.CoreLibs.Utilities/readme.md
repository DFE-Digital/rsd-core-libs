# # GovUK.Dfe.CoreLibs.Utilities

## What does this do ?
This package includes a set of utilities and extension methods.

## How to use it 

1) Install the package.
2) Import the GovUK.Dfe.CoreLibs.Utilities namespace wherever you need to use one of the extensions or utilities.
3) Call the extension or utility you need.

## Rate limiting is per process

`AddRateLimiting<TKey>()` registers `RateLimitStore<TKey>`, an in-memory dictionary. Counts are not shared between replicas and reset when the process restarts, so with N replicas each limit is effectively N times larger. Keys are never removed. Do not rely on it for a security control in a multi-replica service; replace `IRateLimiterFactory<TKey>` with a shared implementation (flexforms-api uses a Redis sliding window, `RedisRateLimiterFactory`).