# DfE.CoreLibs.Security

The DfE.CoreLibs.Security library provides a flexible foundation for managing security in .NET projects, including role-based and claim-based policies, custom requirements, and dynamic claims. It enables consistent, configurable security across applications.

## Installation

To install the DfE.CoreLibs.Security library, use the following command in your .NET project:


    dotnet add package DfE.CoreLibs.Security


## Usage

### Setting Up Authorization Policies and Claims

#### 1\. Service Registration

Use the `AddApplicationAuthorization` extension method to register authorization policies and configure custom claims and requirements. Policies can be defined in the `appsettings.json` file or programmatically, and you can add claim providers to inject claims dynamically.

Here's how to set up the service in `ConfigureServices`:

    
```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddApplicationAuthorization(configuration);
    services.AddCustomClaimProvider<UserProfileClaimProvider>(); // if you need to add custom/ non-security claims
}
```
        

#### 2\. Configuring Policies and Claims in `appsettings.json`

Define your authorization policies in `appsettings.json` under the `Authorization:Policies` section. Each policy can specify required roles, claims, and custom requirements.

**Example configuration:**

    
```json
{
  "Authorization": {
    "Policies": [
      {
        "Name": "CanRead",
        "Operator": "OR",
        "Roles": [ "API.Read" ]
      },
      {
        "Name": "CanReadWrite",
        "Operator": "AND",
        "Roles": [ "API.Read", "API.Write" ]
      },
      {
        "Name": "CanReadWritePlus",
        "Operator": "AND",
        "Roles": [ "API.Read", "API.Write" ],
        "Claims": [
          {
            "Type": "API.PersonalInfo",
            "Values": [ "true" ]
          }
        ]
      }
    ]
  }
}
```
        

#### 3\. Using Policy Customization to add a new Requirement

To create custom requirements, implement the `ICustomAuthorizationRequirement` interface and register them using the `RequirementRegistry`.

For example, let's define a `LocationAccessRequirement` that restricts access to users from a specified location.
    
##### a. Define the `LocationAccessRequirement` class

    
```csharp
public class LocationAccessRequirement : ICustomAuthorizationRequirement
{
    public string Type => "LocationAccess";
    public string RequiredLocation { get; }

    public LocationAccessRequirement(string requiredLocation)
    {
        RequiredLocation = requiredLocation;
    }
}
```

##### b. Create a Handler for the Requirement

    
```csharp
public class LocationAccessHandler : AuthorizationHandler<LocationAccessRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LocationAccessRequirement requirement)
    {
        // Check if the user has a Location claim that matches the required location
        var userLocation = context.User.FindFirst("Location")?.Value;
        if (userLocation == requirement.RequiredLocation)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```
      
 ##### c. Register and add the Requirement and Handler in `Startup.cs`, the Key in the `Dictionary<string, Action<AuthorizationPolicyBuilder>>` is name of the Policy you are adding the Requirement to. If the Policy doesnt exist then a new Policy with this Requirement is created for you.

    
```csharp
    services.AddApplicationAuthorization(configuration, new Dictionary<string, Action<AuthorizationPolicyBuilder>>
    {
        { "LocationAccess", policy =>
            {
                policy.Requirements.Add(new LocationAccessRequirement("Headquarters"));
            }
        }
    });

    services.AddSingleton<IAuthorizationHandler, LocationAccessHandler>();
```
              

#### 4\. Adding Custom Claim Providers

Custom claim providers allow you to fetch claims dynamically based on the user’s identity. Implement `ICustomClaimProvider` to create a custom claim provider and register it in `Startup.cs`.

    
```csharp
public class UserProfileClaimProvider : ICustomClaimProvider
{
    public Task<IEnumerable<Claim>> GetClaimsAsync(ClaimsPrincipal principal)
    {
        var claims = new List<Claim>
        {
            new Claim("FirstName", "John"),
            new Claim("PhoneNumber", "+123456789"),
            new Claim("Location", "Headquarters")
        };
        return Task.FromResult((IEnumerable<Claim>)claims);
    }
}

services.AddCustomClaimProvider<UserProfileClaimProvider>();

```
        
        
#### 5\. Applying Policies in Controllers

Once configured, use `[Authorize(Policy = "PolicyName")]` to apply policies on controllers or specific actions.

    
```csharp
[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    public IActionResult Dashboard() => View();
}

[Authorize(Policy = "LocationAccess")]
public IActionResult HeadquartersContent() => View();
    
```

* * *


### Summary

*   **Flexible Policy Configuration:** Define policies in `appsettings.json`.
*   **Custom Claim Support:** Add dynamic claims to users based on identity, allowing additional user-specific data without modifying core claims.
*   **Custom Requirement Registry:** Register and configure custom requirements dynamically to handle complex security rules.

This setup allows flexible and maintainable authorization control across your .NET application, supporting diverse security needs without hard-coded rules.