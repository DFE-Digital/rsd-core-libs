# GovUK.Dfe.CoreLibs.ApplicationSettings

A generic, database-backed application settings management library for DfE applications. This package provides a consistent way to manage, retrieve, and update application settings using Entity Framework Core and integrates with .NET configuration and dependency injection.

## Features

- Store and retrieve application settings from a database
- Integrates with Microsoft.Extensions.Configuration and Microsoft.Extensions.Options
- Supports caching for improved performance
- Designed for use with .NET 8 and Entity Framework Core

## Installation

Add the NuGet package to your project:

```shell
dotnet add package GovUK.Dfe.CoreLibs.ApplicationSettings
```

Or via Visual Studio NuGet Package Manager.

## Integration Guide

Follow these steps to integrate the package with your existing application:

### 1. Add the ApplicationSetting Entity to Your DbContext

Add a `DbSet<ApplicationSetting>` property to your `DbContext`:

```csharp
public class AppSettingsDbContext : DbContext
{
    public DbSet<ApplicationSetting> ApplicationSettings { get; set; }
    // ...
}
```

### Using ApplicationSettings with an Existing DbContext

You do not need to create a dedicated `DbContext` for application settings.
If you have an existing `DbContext`, simply add a `DbSet<ApplicationSetting>` property and call the `ConfigureApplicationSettings` extension method in your `OnModelCreating` override:

```csharp
public class MyAppDbContext : DbContext
{
    public DbSet<ApplicationSetting> ApplicationSettings { get; set; }
    // ... your other DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure ApplicationSetting entity for use in your existing context
        modelBuilder.ConfigureApplicationSettings(schema: "dbo", tableName: "ApplicationSettings");
        base.OnModelCreating(modelBuilder);
    }
}
```

This ensures the `ApplicationSetting` entity is mapped and configured correctly alongside your other entities.

### 2. Configure the ApplicationSetting Entity

In your `DbContext`, override `OnModelCreating` and use the provided extension method to configure the entity:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Configure ApplicationSetting entity with optional schema and table name
    modelBuilder.ConfigureApplicationSettings(schema: "dbo", tableName: "ApplicationSettings");
    base.OnModelCreating(modelBuilder);
}
```

Or, if you are using options from DI:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ConfigureApplicationSettings(options);
    base.OnModelCreating(modelBuilder);
}
```

#### About the Extension Methods

The package provides two extension methods for configuring the `ApplicationSetting` entity:

- `ConfigureApplicationSettings(this ModelBuilder modelBuilder, string? schema = null, string tableName = "ApplicationSettings")`  
  Configures the entity with the specified schema and table name, setting up keys, indexes, and property constraints.

- `ConfigureApplicationSettings(this ModelBuilder modelBuilder, ApplicationSettingsOptions options)`  
  Configures the entity using options (such as schema) provided via dependency injection.

These methods ensure your database table is correctly structured for storing application settings.

### 3. Register Services in Dependency Injection

In your `Program.cs` or `Startup.cs`:

```csharp
services.AddDbContext<AppSettingsDbContext>(options =>
    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

services.AddApplicationSettings<AppSettingsDbContext>();
```

### 4. Apply Migrations

Generate and apply EF Core migrations to create the `ApplicationSettings` table:

```shell
dotnet ef migrations add AddApplicationSettings
dotnet ef database update
```

### 5. Access Settings in Your Code

Inject `IApplicationSettings` where needed:

```csharp
public class MyService
{
    private readonly IApplicationSettings _settings;

    public MyService(IApplicationSettings settings)
    {
        _settings = settings;
    }

    public async Task<string> GetSettingAsync()
    {
        return await _settings.GetAsync<string>("MySettingKey");
    }
}
```
