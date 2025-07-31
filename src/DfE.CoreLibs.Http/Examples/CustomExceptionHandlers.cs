using DfE.CoreLibs.Http.Interfaces;
using DfE.CoreLibs.Http.Models;
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

    public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
    {
        return new ExceptionResponse
        {
            StatusCode = 422,
            Message = $"Business rule violation: {exception.Message}",
            ExceptionType = "BusinessRuleException",
            Context = context
        };
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

    public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
    {
        return new ExceptionResponse
        {
            StatusCode = 400,
            Message = $"Validation failed: {exception.Message}",
            ExceptionType = "ValidationException",
            Context = context
        };
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

    public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
    {
        if (exception is ResourceNotFoundException resourceException)
        {
            return new ExceptionResponse
            {
                StatusCode = 404,
                Message = $"The requested {resourceException.ResourceType} was not found.",
                ExceptionType = "ResourceNotFoundException",
                Context = context
            };
        }

        return new ExceptionResponse
        {
            StatusCode = 404,
            Message = "The requested resource was not found.",
            ExceptionType = "ResourceNotFoundException",
            Context = context
        };
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

    public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
    {
        // Use context to provide more specific error messages
        var message = context != null && context.TryGetValue("operation", out var operation)
            ? $"Operation '{operation}' failed: {exception.Message}"
            : $"Invalid operation: {exception.Message}";

        return new ExceptionResponse
        {
            StatusCode = 400,
            Message = message,
            ExceptionType = "InvalidOperationException",
            Context = context
        };
    }
} 