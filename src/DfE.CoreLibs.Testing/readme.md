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


### Authorization and Endpoint Security Testing Framework

The **Endpoint Security Testing Framework** is a library designed to help you verify that all your API endpoints have the expected security configurations. 
It ensures that each controller and action has the appropriate authorization attributes and that your application's security policies are consistently enforced.

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

For detailed examples, please refer to the [GitHub DDD-CA-Template repository](https://github.com/DFE-Digital/rsd-ddd-clean-architecture).

* * *