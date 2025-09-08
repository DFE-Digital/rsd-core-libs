using GovUK.Dfe.CoreLibs.Http.Interfaces;
using GovUK.Dfe.CoreLibs.Http.Models;

namespace GovUK.Dfe.CoreLibs.Http.Handlers;

/// <summary>
/// Default exception handler for standard .NET exceptions.
/// This handler has lower priority than custom handlers.
/// </summary>
public class DefaultExceptionHandler : ICustomExceptionHandler
{
    public int Priority => 100;

    public bool CanHandle(Type exceptionType)
    {
        return exceptionType.Name switch
        {
            nameof(ArgumentNullException) => true,
            nameof(ArgumentException) => true,
            nameof(InvalidOperationException) => true,
            nameof(UnauthorizedAccessException) => true,
            nameof(NotImplementedException) => true,
            nameof(FileNotFoundException) => true,
            nameof(DirectoryNotFoundException) => true,
            nameof(TimeoutException) => true,
            _ => false
        };
    }

    public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
    {
        var (statusCode, message) = exception.GetType().Name switch
        {
            nameof(ArgumentNullException) => (400, "Invalid request: Required parameter is missing"),
            nameof(ArgumentException) => (400, "Invalid request: " + exception.Message),
            nameof(InvalidOperationException) => (400, "Invalid operation: " + exception.Message),
            nameof(UnauthorizedAccessException) => (401, "Unauthorized access"),
            nameof(NotImplementedException) => (501, "Feature not implemented"),
            nameof(FileNotFoundException) => (404, "Resource not found"),
            nameof(DirectoryNotFoundException) => (404, "Directory not found"),
            nameof(TimeoutException) => (408, "Request timeout"),
            _ => (500, "An unexpected error occurred")
        };

        return new ExceptionResponse
        {
            StatusCode = statusCode,
            Message = message,
            ExceptionType = exception.GetType().Name,
            Context = context
        };
    }
} 
