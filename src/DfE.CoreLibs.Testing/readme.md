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

For detailed examples, please refer to the [GitHub DDD-CA-Template repository](https://github.com/DFE-Digital/rsd-ddd-clean-architecture).

* * *