using DfE.CoreLibs.Http.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace DfE.CoreLibs.Http.Examples;

/// <summary>
/// Example custom exception for business rule violations.
/// </summary>
[ExcludeFromCodeCoverage]
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
    public BusinessRuleException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Example custom exception for validation failures.
/// </summary>
[ExcludeFromCodeCoverage]
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Example custom exception for resource not found scenarios.
/// </summary>
[ExcludeFromCodeCoverage]
public class ResourceNotFoundException : Exception
{
    public string ResourceType { get; }
    public string ResourceId { get; }

    public ResourceNotFoundException(string resourceType, string resourceId)
        : base($"Resource '{resourceType}' with ID '{resourceId}' was not found.")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}

/// <summary>
/// Example custom exception handler for business rule exceptions.
/// This demonstrates how a service can register its own exception handling logic.
/// </summary>
[ExcludeFromCodeCoverage]
public class BusinessRuleExceptionHandler : ICustomExceptionHandler
{
    public int Priority => 10; // Higher priority than default handler

    public bool CanHandle(Type exceptionType)
    {
        return exceptionType == typeof(BusinessRuleException);
    }

    public (int statusCode, string message) Handle(Exception exception, Dictionary<string, object>? context = null)
    {
        return (422, $"Business rule violation: {exception.Message}");
    }
}

/// <summary>
/// Example custom exception handler for validation exceptions.
/// </summary>
[ExcludeFromCodeCoverage]
public class ValidationExceptionHandler : ICustomExceptionHandler
{
    public int Priority => 20; // Higher priority than default handler

    public bool CanHandle(Type exceptionType)
    {
        return exceptionType == typeof(ValidationException);
    }

    public (int statusCode, string message) Handle(Exception exception, Dictionary<string, object>? context = null)
    {
        return (400, $"Validation failed: {exception.Message}");
    }
}

/// <summary>
/// Example custom exception handler for resource not found exceptions.
/// </summary>
[ExcludeFromCodeCoverage]
public class ResourceNotFoundExceptionHandler : ICustomExceptionHandler
{
    public int Priority => 30; // Higher priority than default handler

    public bool CanHandle(Type exceptionType)
    {
        return exceptionType == typeof(ResourceNotFoundException);
    }

    public (int statusCode, string message) Handle(Exception exception, Dictionary<string, object>? context = null)
    {
        if (exception is ResourceNotFoundException resourceException)
        {
            return (404, $"The requested {resourceException.ResourceType} was not found.");
        }

        return (404, "The requested resource was not found.");
    }
}

/// <summary>
/// Example custom exception handler that demonstrates context usage.
/// </summary>
[ExcludeFromCodeCoverage]
public class ContextAwareExceptionHandler : ICustomExceptionHandler
{
    public int Priority => 5; // Very high priority

    public bool CanHandle(Type exceptionType)
    {
        return exceptionType == typeof(InvalidOperationException);
    }

    public (int statusCode, string message) Handle(Exception exception, Dictionary<string, object>? context = null)
    {
        // Use context to provide more specific error messages
        if (context != null && context.TryGetValue("operation", out var operation))
        {
            return (400, $"Operation '{operation}' failed: {exception.Message}");
        }

        return (400, $"Invalid operation: {exception.Message}");
    }
} 