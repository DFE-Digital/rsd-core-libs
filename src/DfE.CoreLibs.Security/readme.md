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
        "Roles": [ "Reader" ],
        "Scopes": [ "SCOPE.API.Read" ]
      },
      {
        "Name": "CanReadWrite",
        "Operator": "AND",
        "Roles": [ "Reader", "Writer" ],
        "Scopes": [ "SCOPE.API.Read", "SCOPE.API.Write" ]
      },
      {
        "Name": "CanReadWriteDelete",
        "Operator": "AND",
        "Roles": [ "Reader", "Writer", "Deleter" ],
        "Scopes": [ "SCOPE.API.Read", "SCOPE.API.Write", "SCOPE.API.Delete" ],
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


**Key Points:**

*   **Policy Names**: Each policy must have a unique `Name`.
*   **Operator**: Defines the logical operation (`AND` or `OR`) used to evaluate Roles and Scopes.
*   **Roles**: A list of roles that can satisfy the policy based on the operator.
*   **Scopes**: A list of scopes that can satisfy the policy based on the operator. **Note**: Scopes are equivalent to roles but are primarily used in API scenarios, especially with OBO tokens.
*   **Claims**: Additional claim requirements that must be met for the policy to succeed.

### 3\. Creating Authorization Policies

For each policy defined in the configuration:

*   **Operator Logic**:
    *   `AND`:
        *   **With Scopes**: The user must have **all** specified scopes.
        *   **Without Scopes**: The user must have **all** specified roles.
    *   `OR`:
        *   The user must have **at least one** of the specified scopes **or** **at least one** of the specified roles.
*   **Claims**: If the policy includes claims, the user must possess the specified claims.

**Note**: Scopes are only needed if this configuration is for an API and the API will be accessed by another client and is provided with an OBO Token.


### Understanding Scopes and Roles in Authorization Policies

**Scopes vs. Roles**

*   **Roles**:
    *   **Definition**: Represent broader permissions or access levels within an application.
    *   **Usage**: Commonly used in traditional authentication scenarios where users are assigned roles that determine their access rights.
    *   **Example**: `Reader`, `Writer`, `Admin`.
*   **Scopes**:
    *   **Definition**: Represent granular permissions, primarily used in OAuth 2.0 and OpenID Connect scenarios.
    *   **Usage**: Employed in API-centric architectures where tokens (like JWTs) contain scopes that define the level of access granted to client applications.
    *   **Example**: `SCOPE.API.Read`, `SCOPE.API.Write`.

**Key Equivalence:** Within authorization policies, **Scopes** and **Roles** are treated equivalently. A policy can require either specific roles or scopes, depending on the user's token type.


**When to Use Scopes**


*   **API Scenarios**:
    *   If your application serves as an **API** that exposes **Scopes**, use scopes within your authorization policies.
    *   Scopes are particularly relevant when dealing with **On-Behalf-Of (OBO)** tokens, where scopes are included instead of roles.
*   **OBO Tokens**:
    *   **Definition**: OBO tokens allow a service (like an API) to act on behalf of a user, typically used in multi-tiered applications.
    *   **Behavior**: These tokens contain **Scopes** instead of **Roles**, representing the delegated permissions.
    *   **Policy Implication**: Authorization policies should be configured to recognize and evaluate scopes appropriately when processing OBO tokens.



#### 4\. Using Policy Customization to add a new Requirement

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
              

#### 5\. Adding Custom Claim Providers

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
        
        
#### 6\. Applying Policies in Controllers

Once configured, use `[Authorize(Policy = "PolicyName")]` to apply policies on controllers or specific actions.

    
```csharp
[Authorize(Policy = "CanReadWrite")]
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