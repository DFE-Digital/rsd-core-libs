# DfE.CoreLibs.Caching

This caching library offers a unified, efficient caching solution for .NET projects. It provides a simple, reusable abstraction over different caching mechanisms, enabling developers to easily implement in-memory and distributed caching strategies, improving the performance and scalability of their applications.

## Installation

To install the DfE.CoreLibs.Caching Library, use the following command in your .NET project:

```sh
dotnet add package DfE.CoreLibs.Caching
```

## Usage

**Usage in Handlers**

1.  **Service Registration:** You use `ICacheService` in your handlers to store and retrieve data from memory to avoid unnecessary processing and database queries. Here's how you register the caching service:

    ```csharp
        public void ConfigureServices(IServiceCollection services)
        {
                services.AddServiceCaching(config);
        }
    ```

2.  **Usage in Handlers:** Here's an example of how caching is used in one of your query handlers:

    ```csharp
    public class GetPrincipalBySchoolQueryHandler(
        ISchoolRepository schoolRepository,
        IMapper mapper,
        ICacheService cacheService)
        : IRequestHandler<GetPrincipalBySchoolQuery, Principal?>
    {
        public async Task<Principal?> Handle(GetPrincipalBySchoolQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"Principal_{CacheKeyHelper.GenerateHashedCacheKey(request.SchoolName)}";

            var methodName = nameof(GetPrincipalBySchoolQueryHandler);

            return await cacheService.GetOrAddAsync(cacheKey, async () =>
            {
                var principal= await schoolRepository
                    .GetPrincipalBySchoolAsync(request.SchoolName, cancellationToken);

                var result = mapper.Map<Principal?>(principal);

                return result;
            }, methodName);
        }
    }
    ```

In this case, the query handler checks if the principals are cached by generating a unique cache key. If the data is not cached, it retrieves the data from the repository, caches it, and returns it.

### Cache Duration Based on Method Name

The caching service dynamically determines the cache duration based on the method name. This is particularly useful when you want to apply different caching durations to different query handlers.
In this example, the cache duration for `GetPrincipalBySchoolQueryHandler` is retrieved from the configuration using the method name. If no specific duration is defined for the method, it will fall back to the default cache duration.

#### Example of Cache Settings in appsettings.json

Here is the configuration for cache durations in the `appsettings.json` file:

    ```csharp
    "CacheSettings": {
        "DefaultDurationInSeconds": 60,
        "Durations": {
          "GetPrincipalBySchoolQueryHandler": 86400
        }
    }
    ```

This setup ensures that the `GetPrincipalBySchoolQueryHandler` cache duration is set to 24 hours (86400 seconds), while other handlers will use the default duration of 60 seconds if no specific duration is configured.

* * *