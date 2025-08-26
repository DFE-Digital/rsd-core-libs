# DfE.CoreLibs.Caching

This caching library offers a unified, efficient caching solution for .NET projects. It provides a simple, reusable abstraction over different caching mechanisms, enabling developers to easily implement in-memory and distributed caching strategies, improving the performance and scalability of their applications.

## Installation

To install the DfE.CoreLibs.Caching Library, use the following command in your .NET project:

```sh
dotnet add package DfE.CoreLibs.Caching
```

## Caching Options

This library supports multiple caching strategies:

1. **Memory Caching** - Fast, in-process caching suitable for single-instance applications
2. **Redis Caching** - Distributed caching suitable for multi-instance applications and microservices
3. **Hybrid Caching** - Use both memory and Redis caching in the same application

## Service Registration

### Memory Caching Only

For applications that only need in-memory caching:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceCaching(config);
}
```

### Redis Caching Only

For applications that need distributed caching:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddRedisCaching(config);
}
```

### Hybrid Caching

For applications that need both memory and Redis caching:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHybridCaching(config);
}
```

## Usage in Handlers

### Memory Cache Usage

```csharp
public class GetPrincipalBySchoolQueryHandler(
    ISchoolRepository schoolRepository,
    IMapper mapper,
    ICacheService<IMemoryCacheType> cacheService)
    : IRequestHandler<GetPrincipalBySchoolQuery, Principal?>
{
    public async Task<Principal?> Handle(GetPrincipalBySchoolQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"Principal_{CacheKeyHelper.GenerateHashedCacheKey(request.SchoolName)}";
        var methodName = nameof(GetPrincipalBySchoolQueryHandler);

        return await cacheService.GetOrAddAsync(cacheKey, async () =>
        {
            var principal = await schoolRepository
                .GetPrincipalBySchoolAsync(request.SchoolName, cancellationToken);

            return mapper.Map<Principal?>(principal);
        }, methodName);
    }
}
```

### Redis Cache Usage

```csharp
public class GetSchoolDataQueryHandler(
    ISchoolRepository schoolRepository,
    IMapper mapper,
    ICacheService<IRedisCacheType> redisCacheService)
    : IRequestHandler<GetSchoolDataQuery, SchoolData?>
{
    public async Task<SchoolData?> Handle(GetSchoolDataQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"School_{CacheKeyHelper.GenerateHashedCacheKey(request.SchoolId.ToString())}";
        var methodName = nameof(GetSchoolDataQueryHandler);

        return await redisCacheService.GetOrAddAsync(cacheKey, async () =>
        {
            var schoolData = await schoolRepository
                .GetSchoolDataAsync(request.SchoolId, cancellationToken);

            return mapper.Map<SchoolData?>(schoolData);
        }, methodName);
    }
}
```

### Hybrid Cache Usage

When using hybrid caching, you can inject both cache services and choose which one to use based on your needs:

```csharp
public class GetUserDataQueryHandler(
    IUserRepository userRepository,
    IMapper mapper,
    ICacheService<IMemoryCacheType> memoryCacheService,
    ICacheService<IRedisCacheType> redisCacheService)
    : IRequestHandler<GetUserDataQuery, UserData?>
{
    public async Task<UserData?> Handle(GetUserDataQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"User_{CacheKeyHelper.GenerateHashedCacheKey(request.UserId.ToString())}";
        var methodName = nameof(GetUserDataQueryHandler);

        // Use memory cache for frequently accessed, small data
        if (request.UseMemoryCache)
        {
            return await memoryCacheService.GetOrAddAsync(cacheKey, () => FetchUserData(request), methodName);
        }

        // Use Redis cache for larger data or when sharing between instances
        return await redisCacheService.GetOrAddAsync(cacheKey, () => FetchUserData(request), methodName);
    }

    private async Task<UserData?> FetchUserData(GetUserDataQuery request)
    {
        var userData = await userRepository.GetUserDataAsync(request.UserId);
        return mapper.Map<UserData?>(userData);
    }
}
```

### GetAsync Usage (Retrieve Only)

Sometimes you may want to retrieve cached data without adding it to the cache if it doesn't exist. Use the `GetAsync` method for this purpose:

```csharp
public class GetCachedUserQueryHandler(
    ICacheService<IMemoryCacheType> memoryCacheService,
    ICacheService<IRedisCacheType> redisCacheService)
    : IRequestHandler<GetCachedUserQuery, UserData?>
{
    public async Task<UserData?> Handle(GetCachedUserQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"User_{CacheKeyHelper.GenerateHashedCacheKey(request.UserId.ToString())}";
        var methodName = nameof(GetCachedUserQueryHandler);

        // Try to get from memory cache first
        var cachedData = await memoryCacheService.GetAsync<UserData>(cacheKey, methodName);
        
        // If not in memory cache, try Redis cache
        if (cachedData == null)
        {
            cachedData = await redisCacheService.GetAsync<UserData>(cacheKey, methodName);
        }

        return cachedData; // Returns null if not found in either cache
    }
}
```

## Cache Duration Based on Method Name

The caching service dynamically determines the cache duration based on the method name. This is particularly useful when you want to apply different caching durations to different query handlers.

## Configuration

### Complete Configuration Example

Here is a complete configuration example in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "CacheSettings": {
    "Memory": {
      "DefaultDurationInSeconds": 60,
      "Durations": {
        "GetPrincipalBySchoolQueryHandler": 86400,
        "GetUserDataQueryHandler": 300
      }
    },
    "Redis": {
      "DefaultDurationInSeconds": 300,
      "KeyPrefix": "MyApp:Cache:",
      "Database": 0,
      "Durations": {
        "GetSchoolDataQueryHandler": 1800,
        "GetUserDataQueryHandler": 600
      }
    }
  }
}
```

### Memory Cache Settings

- `DefaultDurationInSeconds`: Default cache duration for memory cache (default: 5 seconds)
- `Durations`: Method-specific cache durations

### Redis Cache Settings

#### Connection String Configuration

The Redis connection string can be configured in two ways with the following priority:

1. **ConnectionStrings:Redis** (Recommended - follows .NET conventions)
2. **CacheSettings:Redis:ConnectionString** (Fallback)

If both are configured, `ConnectionStrings:Redis` takes priority over `CacheSettings:Redis:ConnectionString`.

**Example using ConnectionStrings (Recommended):**
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "CacheSettings": {
    "Redis": {
      "DefaultDurationInSeconds": 300,
      "KeyPrefix": "MyApp:Cache:",
      "Database": 0
    }
  }
}
```

**Example using CacheSettings (Fallback):**
```json
{
  "CacheSettings": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "DefaultDurationInSeconds": 300,
      "KeyPrefix": "MyApp:Cache:",
      "Database": 0
    }
  }
}
```

#### Other Settings

- `DefaultDurationInSeconds`: Default cache duration for Redis cache (default: 300 seconds)
- `KeyPrefix`: Prefix for all Redis cache keys (default: "DfE:Cache:")
- `Database`: Redis database number (default: 0)
- `Durations`: Method-specific cache durations

## Advanced Redis Features

The Redis cache service provides additional features not available in memory caching:

### Async Remove Operations

```csharp
// Remove a single key asynchronously
await redisCacheService.RemoveAsync(cacheKey);

// Remove multiple keys by pattern
await redisCacheService.RemoveByPatternAsync("User_*");
```

### Error Handling

The Redis cache service includes built-in error handling:

- **Redis Connection Failures**: Automatically falls back to the fetch function
- **JSON Serialization Errors**: Removes invalid cache entries and refetches data
- **Comprehensive Logging**: All operations and errors are logged

## Best Practices

1. **Choose the Right Cache Type**:
   - Use memory caching for small, frequently accessed data in single-instance applications
   - Use Redis caching for larger data, distributed applications, or when data needs to be shared between instances
   - Use hybrid caching when you need both strategies in the same application

2. **Cache Key Management**:
   - Always use `CacheKeyHelper.GenerateHashedCacheKey()` for consistent, secure cache keys
   - Include relevant parameters in cache keys to ensure proper data isolation

3. **Cache Duration Strategy**:
   - Set shorter durations for frequently changing data
   - Use method-specific durations for fine-grained control
   - Consider your application's data consistency requirements

4. **Error Handling**:
   - Redis cache service automatically handles connection failures
   - Monitor logs for cache performance and error patterns

* * *