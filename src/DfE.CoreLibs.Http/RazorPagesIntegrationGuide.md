# Razor Pages Integration Guide

This guide shows how to integrate the Global Exception Handler Middleware with Razor Pages applications that use MediatR and NSwag clients.

## Overview

The middleware can handle exceptions from:
- **MediatR ValidationException**: From FluentValidation in your commands/queries
- **ExternalApplicationsException**: From NSwag-generated API clients
- **Any other custom exceptions**: Through custom exception handlers

## Quick Setup

### 1. Install the Package

```bash
dotnet add package DfE.CoreLibs.Http
```

### 2. Configure in Program.cs

```csharp
using DfE.CoreLibs.Http.Extensions;
using DfE.CoreLibs.Http.Examples;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddMediatR(typeof(Program));
builder.Services.AddHttpClient<YourApiClient>();

// Configure exception handler
builder.Services.ConfigureGlobalExceptionHandler(options =>
{
    options.IncludeDetails = builder.Environment.IsDevelopment();
    options.LogExceptions = true;
    options.DefaultErrorMessage = "An error occurred. Please try again.";
});

// Register custom exception handlers
builder.Services.AddCustomExceptionHandlers(
    new EnhancedValidationExceptionHandler(), // Handles ValidationException from MediatR
    new ExternalApplicationsExceptionHandler() // Handles ExternalApplicationsException from NSwag
);

var app = builder.Build();

// Configure pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Add exception handler middleware
app.UseGlobalExceptionHandler(options =>
{
    options.WithEnvironmentAwareErrorIds(app.Environment.EnvironmentName)
           .WithSharedPostProcessing((exception, response) =>
           {
               // Add request context
               response.Context ??= new Dictionary<string, object>();
               response.Context["RequestPath"] = app.HttpContext?.Request.Path;
               response.Context["UserAgent"] = app.HttpContext?.Request.Headers.UserAgent.ToString();
           });
});

app.MapRazorPages();
app.Run();
```

## Exception Handling Examples

### 1. ValidationException from MediatR

When your MediatR command/query throws a `ValidationException`, it will be automatically handled:

```csharp
// Your command
public class CreateUserCommand : IRequest<CreateUserResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

// Your validator
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).EmailAddress().NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

// Your Razor Page
public class CreateUserModel : PageModel
{
    private readonly IMediator _mediator;

    public CreateUserModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var result = await _mediator.Send(new CreateUserCommand
            {
                Email = Email,
                Name = Name
            });

            return RedirectToPage("./Success");
        }
        catch (ValidationException)
        {
            // The middleware will handle this and return a 422 with validation details
            // You can also handle it locally if needed
            return Page();
        }
    }
}
```

**Response Format:**
```json
{
  "errorId": "D-123456",
  "statusCode": 422,
  "message": "Validation failed: Email: Email is required; Name: Name is required",
  "exceptionType": "ValidationException",
  "timestamp": "2024-01-15T10:30:00.000Z",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "context": {
    "ValidationErrors": {
      "Email": ["Email is required"],
      "Name": ["Name is required"]
    }
  }
}
```

### 2. ExternalApplicationsException from NSwag

When your NSwag client throws an `ExternalApplicationsException`, it will be automatically handled:

```csharp
public class UserService
{
    private readonly IApiClient _apiClient;

    public UserService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<User> GetUserAsync(int id)
    {
        try
        {
            return await _apiClient.GetUserAsync(id);
        }
        catch (ExternalApplicationsException ex)
        {
            // The middleware will handle this and extract the API error details
            throw;
        }
    }
}
```

**Response Format:**
```json
{
  "errorId": "D-123456",
  "statusCode": 404,
  "message": "User not found",
  "exceptionType": "ExternalApplicationsException",
  "timestamp": "2024-01-15T10:30:00.000Z",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000"
}
```

## JavaScript/AJAX Integration

### 1. Handle Errors in JavaScript

```javascript
async function createUser(userData) {
    try {
        const response = await fetch('/CreateUser?handler=Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify(userData)
        });

        if (!response.ok) {
            const errorData = await response.json();
            
            // Handle standardized error response
            if (errorData.errorId) {
                console.error(`Error ${errorData.errorId}: ${errorData.message}`);
                
                // Display validation errors if available
                if (errorData.context?.validationErrors) {
                    displayValidationErrors(errorData.context.validationErrors);
                }
                
                // Show user-friendly error message
                showErrorMessage(errorData.message);
            }
        } else {
            // Success handling
            window.location.href = '/Success';
        }
    } catch (error) {
        console.error('Request failed:', error);
        showErrorMessage('An unexpected error occurred.');
    }
}

function displayValidationErrors(validationErrors) {
    // Clear previous errors
    document.querySelectorAll('.field-error').forEach(el => el.remove());
    
    // Display new errors
    Object.keys(validationErrors).forEach(field => {
        const fieldElement = document.getElementById(field);
        if (fieldElement) {
            validationErrors[field].forEach(error => {
                const errorElement = document.createElement('div');
                errorElement.className = 'field-error';
                errorElement.textContent = error;
                fieldElement.parentNode.appendChild(errorElement);
            });
        }
    });
}

function showErrorMessage(message) {
    const errorContainer = document.getElementById('error-message');
    if (errorContainer) {
        errorContainer.textContent = message;
        errorContainer.style.display = 'block';
    }
}
```

### 2. Global Error Handler

```javascript
// Add to your layout or main JavaScript file
window.addEventListener('error', function(event) {
    console.error('Global error:', event.error);
    
    // You can send errors to your monitoring system
    if (typeof gtag !== 'undefined') {
        gtag('event', 'exception', {
            description: event.error?.message || 'Unknown error',
            fatal: false
        });
    }
});

// Handle unhandled promise rejections
window.addEventListener('unhandledrejection', function(event) {
    console.error('Unhandled promise rejection:', event.reason);
    event.preventDefault();
});
```

## Custom Error Pages

### 1. Create Error.cshtml

```html
@page
@model ErrorModel

<div class="error-container">
    <h1>Error @Model.ErrorId</h1>
    <p>@Model.Message</p>
    
    @if (Model.Context?.ContainsKey("ValidationErrors") == true)
    {
        <div class="validation-errors">
            <h3>Validation Errors:</h3>
            <ul>
            @foreach (var error in Model.Context["ValidationErrors"] as Dictionary<string, string[]>)
            {
                <li><strong>@error.Key:</strong> @string.Join(", ", error.Value)</li>
            }
            </ul>
        </div>
    }
    
    @if (!string.IsNullOrEmpty(Model.CorrelationId))
    {
        <p><small>Correlation ID: @Model.CorrelationId</small></p>
    }
    
    <a href="/" class="btn btn-primary">Return to Home</a>
</div>
```

### 2. Create Error.cshtml.cs

```csharp
using System.Text.Json;

public class ErrorModel : PageModel
{
    public string ErrorId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Context { get; set; }
    public string? CorrelationId { get; set; }

    public void OnGet()
    {
        // Extract error information from TempData
        ErrorId = TempData["ErrorId"]?.ToString() ?? "Unknown";
        Message = TempData["ErrorMessage"]?.ToString() ?? "An error occurred";
        CorrelationId = TempData["CorrelationId"]?.ToString();
        
        // Parse context if available
        if (TempData["ErrorContext"] is string contextJson)
        {
            try
            {
                Context = JsonSerializer.Deserialize<Dictionary<string, object>>(contextJson);
            }
            catch
            {
                // Handle parsing error
            }
        }
    }
}
```

## Environment-Specific Configuration

### 1. Development Environment

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseGlobalExceptionHandler(options =>
    {
        options.WithEnvironmentAwareErrorIds("Development") // D-123456
               .WithSharedPostProcessing((exception, response) =>
               {
                   // Development: Log to console and show detailed errors
                   Console.WriteLine($"DEV ERROR: {response.ErrorId} - {response.Message}");
                   response.Details = exception.ToString();
               });
    });
}
```

### 2. Production Environment

```csharp
else
{
    app.UseGlobalExceptionHandler(options =>
    {
        options.WithEnvironmentAwareErrorIds("Production") // P-123456
               .WithSharedPostProcessing((exception, response) =>
               {
                   // Production: Send to monitoring system
                   SendToMonitoringSystem(response);
                   
                   // Hide sensitive details
                   response.Details = null;
               });
    });
}
```

## Testing

### 1. Unit Testing

```csharp
[Fact]
public async Task Should_Handle_ValidationException_Correctly()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.PostAsJsonAsync("/CreateUser", new { });
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    var content = await response.Content.ReadAsStringAsync();
    var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(content);
    
    errorResponse.Should().NotBeNull();
    errorResponse!.ErrorId.Should().MatchRegex(@"^[DTPUQX]-\d{6}$");
    errorResponse.StatusCode.Should().Be(422);
    errorResponse.Context.Should().ContainKey("ValidationErrors");
}
```

### 2. Integration Testing

```csharp
[Fact]
public async Task Should_Handle_ExternalApplicationsException_Correctly()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Mock API client to throw ExternalApplicationsException
    _mockApiClient.Setup(x => x.GetUserAsync(It.IsAny<int>()))
                 .ThrowsAsync(new ExternalApplicationsException("User not found", 404, "User not found", null, null));
    
    // Act
    var response = await client.GetAsync("/User/1");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    var content = await response.Content.ReadAsStringAsync();
    var errorResponse = JsonSerializer.Deserialize<ExceptionResponse>(content);
    
    errorResponse.Should().NotBeNull();
    errorResponse!.Message.Should().Contain("User not found");
}
```

## Best Practices

### 1. Error Handling Strategy

- **Validation Errors**: Use 422 status code with detailed field errors
- **API Errors**: Preserve original status code and message
- **System Errors**: Use 500 status code with generic message in production

### 2. Logging

- **Development**: Log full exception details to console
- **Production**: Log to structured logging system with error IDs
- **Monitoring**: Send critical errors to monitoring system

### 3. User Experience

- **Validation Errors**: Display field-specific errors
- **API Errors**: Show user-friendly messages
- **System Errors**: Show generic error message with error ID for support

### 4. Security

- **Development**: Include full exception details
- **Production**: Hide sensitive information
- **Error IDs**: Use for tracking but don't expose system details

## Troubleshooting

### 1. Common Issues

**Issue**: ValidationException not being caught
**Solution**: Ensure the exception handler is registered before your endpoints

**Issue**: ExternalApplicationsException not being parsed correctly
**Solution**: Check that the response property contains valid JSON

**Issue**: Error context not being passed to frontend
**Solution**: Ensure the context dictionary is properly populated in the handler

### 2. Debugging

```csharp
// Add debug logging to your handlers
public (int statusCode, string message) Handle(Exception exception, Dictionary<string, object>? context = null)
{
    _logger.LogDebug("Handling {ExceptionType} with context: {@Context}", 
        exception.GetType().Name, context);
    
    // Your handler logic here
}
```

This integration guide provides everything you need to handle exceptions from MediatR and NSwag clients in your Razor Pages application with a clean, standardized approach. 