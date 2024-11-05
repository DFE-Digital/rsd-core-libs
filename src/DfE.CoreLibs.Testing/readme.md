# DfE.CoreLibs.Testing

Designed to enhance test automation, this library provides essential utilities and frameworks for unit and integration testing in .NET. It includes tools for mocking, assertions, and common test scenarios, helping developers write cleaner, more efficient tests that improve the overall quality and stability of their applications.

## Installation

To install the DfE.CoreLibs.Testing Library, use the following command in your .NET project:

```sh
dotnet add package DfE.CoreLibs.Testing
```

## Usage

### Usage of Customization Attributes

In your tests, you can use `CustomAutoData` to easily inject customizations like `AutoMapperCustomization`, this Customization scans your assembly for profiles and registers them automatically.

```csharp
    [Theory]
    [CustomAutoData(
        typeof(PrincipalCustomization),
        typeof(SchoolCustomization),
        typeof(AutoMapperCustomization<SchoolProfile>))]
    public async Task Handle_ShouldReturnMemberOfParliament_WhenSchoolExists(
        [Frozen] ISchoolRepository mockSchoolRepository,
        [Frozen] ICacheService mockCacheService,
        GetPrincipalBySchoolQueryHandler handler,
        GetPrincipalBySchoolQuery query,
        Domain.Entities.Schools.School school,
        IFixture fixture)
    {
        // Arrange
        var expectedMp = fixture.Customize(new PrincipalCustomization()
        {
            FirstName = school.NameDetails.NameListAs!.Split(",")[1].Trim(),
            LastName = school.NameDetails.NameListAs.Split(",")[0].Trim(),
            SchoolName = school.SchoolName,
        }).Create<Principal>();
    
        // Act
        var result = await handler.Handle(query, default);
    
        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedMp.FirstName, result.FirstName);
        Assert.Equal(expectedMp.LastName, result.LastName);
    }
```

### CustomWebApplicationDbContextFactory

You can create custom factory customizations and use them like the following example, which demonstrates testing with a custom web application factory:

```csharp
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization<Program, SclContext>))]
    public async Task GetPrincipalBySchoolAsync_ShouldReturnPrincipal_WhenSchoolExists(
        CustomWebApplicationDbContextFactory<Program, SclContext> factory,
        ISchoolsClient schoolsClient)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, "API.Read")];
    
        // Arrange
        var dbContext = factory.GetDbContext();
    
        await dbContext.Schools
            .Where(x => x.SchoolName == "Test School 1")
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.SchoolName, "NewSchoolName"));
    
        var schoolName = Uri.EscapeDataString("NewSchoolName");
    
        // Act
        var result = await schoolsClient.GetPrincipalBySchoolAsync(schoolName);
    
        // Assert
        Assert.NotNull(result);
        Assert.Equal("NewSchoolName", result.SchoolName);
    }
```

This demonstrates how you can test your queries and database context interactions using a custom web application factory and test claims.


## Authorization and Endpoint and Page Security Testing Framework

The **Endpoint and Page Security Testing Framework** is a library designed to help you verify that all your API endpoints have the expected security configurations. 
It ensures that each controller and action has the appropriate authorization attributes and that your application's security policies are consistently enforced.

## Endpoint Security Validator

**Endpoint Security Validator** allows you to validate that endpoint in your .NET API  has the correct security settings. The validator uses reflection along with a configuration file to enforce expected security requirements.

## Usage

To utilize the framework, follow these steps:

### 1\. Create the Security Configuration File

Create a JSON file (e.g., `ExpectedSecurity.json`) that defines the expected security for each endpoint in your application. This file should include all controllers and actions.


```json
{
    "Endpoints": [
    {
        "Controller": "SchoolsController",
        "Action": "GetPrincipalBySchoolAsync",
        "ExpectedSecurity": "Authorize: Policy=API.Read"
    },
    {
        "Controller": "SchoolsController",
        "Action": "GetPrincipalsBySchoolsAsync",
        "ExpectedSecurity": "Authorize: Policy=API.Read"
    },
    {
        "Controller": "SchoolsController",
        "Action": "CreateSchoolAsync",
        "ExpectedSecurity": "Authorize: Policy=API.Write"
    },
    {
        "Controller": "SchoolsController",
        "Action": "CreateReportAsync",
        "ExpectedSecurity": "AllowAnonymous"
    }
    ]
}
```


### 2\. Write the Test Class

Create a test class in your test project that uses the framework to validate your endpoints.

```csharp
    public class EndpointSecurityTests
    {
        [Theory]
        [MemberData(nameof(GetEndpointTestData))]
        public void ValidateEndpointSecurity(string controllerName, string actionName, string expectedSecurity)
        {
            var securityTests = new AuthorizationTester();

            securityTests.ValidateEndpoint(typeof(Program).Assembly, controllerName, actionName, expectedSecurity);
        }

        public static IEnumerable<object[]> GetEndpointTestData()
        {
            var configFilePath = "ExpectedSecurity.json";
            return EndpointTestDataProvider.GetEndpointTestDataFromFile(typeof(Program).Assembly, configFilePath);
        }
    }
```

The above test will run a test per endpoint and ensures the expected security policy is applied to thje endpoint or the controller.

## Page Security Validator

**Page Security Validator** allows you to validate that each page in your ASP.NET Core application has the correct security settings. The validator uses route metadata along with a configuration file to enforce expected security requirements, including global authorization settings or route-specific configurations.

## Usage

### 1\. Create the Security Configuration File

```json
{
    "Endpoints": [
        {
            "Route": "/public/accessibility",
            "ExpectedSecurity": "AllowAnonymous"
        },
        {
            "Route": "/admin/dashboard",
            "ExpectedSecurity": "Authorize: Policy=AdminOnly"
        },
        {
            "Route": "/user/profile",
            "ExpectedSecurity": "Authorize: Roles=User,Manager"
        }
    ]
}
```
    
    This configuration file should be set to always copy to the output directory by setting `Copy to Output Directory` to `Copy always` in your project settings.
    

### Understanding `_globalAuthorizationEnabled`


*   **When `_globalAuthorizationEnabled` is `true`:**
    *   This setting assumes **global security enforcement** is applied in `Startup.cs` (e.g., `AuthorizeFolder("/")`).
    *   By default, **all pages are expected to have the `Authorize` attribute**.
    *   The configuration file can specify exceptions to global authorization, such as `AllowAnonymous` or specific authorization policies or roles.
*   **When `_globalAuthorizationEnabled` is `false`:**
    *   Only the routes explicitly listed in the configuration file are validated.
    *   No global assumptions are made about other pages.


### 2\. Test Setup

The test setup includes:

*   Instantiating the `AuthorizationTester`.
*   Using `InitializeEndpoints` to retrieve all relevant endpoints.
*   Loading security expectations from the JSON configuration file.

### Test Class Structure

```csharp
    public class PageSecurityTests
    {
        private readonly AuthorizationTester _validator;
        private static readonly Lazy<IEnumerable<RouteEndpoint>> _endpoints = new(InitializeEndpoints);
        private const bool _globalAuthorizationEnabled = true;

        public PageSecurityTests()
        {
            _validator = new AuthorizationTester(_globalAuthorizationEnabled);
        }

        [Theory]
        [MemberData(nameof(GetPageSecurityTestData))]
        public void ValidatePageSecurity(string route, string expectedSecurity)
        {
            var result = _validator.ValidatePageSecurity(route, expectedSecurity, _endpoints.Value);
            Assert.Null(result.Message);
        }

        public static IEnumerable<object[]> GetPageSecurityTestData()
        {
            var configFilePath = "ExpectedSecurityConfig.json";
            return EndpointTestDataProvider.GetPageSecurityTestDataFromFile(configFilePath, _endpoints.Value, _globalAuthorizationEnabled);
        }

        private static IEnumerable<RouteEndpoint> InitializeEndpoints()
        {
            // Using a temporary factory to access the EndpointDataSource for lazy initialization
            var factory = new CustomWebApplicationFactory<Startup>();
            var endpointDataSource = factory.Services.GetRequiredService<EndpointDataSource>();

            return endpointDataSource.Endpoints
                .OfType<RouteEndpoint>()
                .Where(x => x.DisplayName!.Contains("Public/"));
        }
    }
```

### Explanation of Key Components

*   **`AuthorizationTester` Instance:** Instantiates the validator with `_globalAuthorizationEnabled`.
*   **Lazy Initialization of Endpoints:** `_endpoints` defers endpoint retrieval until needed, using `InitializeEndpoints`.
*   **Test Method (`ValidatePageSecurity`):** Checks each route against its expected security settings.
*   **`GetPageSecurityTestData`:** Loads security settings from the configuration file.
*   **`InitializeEndpoints`:** Retrieves all relevant `RouteEndpoint` instances.

### Expected Security Configuration (JSON)

Each route entry in the JSON file specifies:

*   **Route:** The URL pattern or path of the page.
*   **ExpectedSecurity:** The required security setting:
    *   `AllowAnonymous` for public pages.
    *   `Authorize` with optional `Policy` or `Roles` for restricted pages.


### 3\. Running the Tests

Run the `PageSecurityTests` test suite to verify that each page’s security matches the specified expectations.

If a route is missing the expected security setting, `Assert.Null(result.Message);` will fail and show the error message, such as:

    Page /admin/dashboard should be protected with Policy 'AdminOnly' but was not found.


For detailed examples, please refer to the [GitHub DDD-CA-Template repository](https://github.com/DFE-Digital/rsd-ddd-clean-architecture).

* * *