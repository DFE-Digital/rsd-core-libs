namespace DfE.CoreLibs.Http.Interfaces;

/// <summary>
/// Interface for custom exception handlers that can be registered with the global exception handler middleware.
/// Implement this interface to handle custom exceptions from your services.
/// </summary>
public interface ICustomExceptionHandler
{
    /// <summary>
    /// Determines if this handler can handle the specified exception type.
    /// </summary>
    /// <param name="exceptionType">The type of exception to check.</param>
    /// <returns>True if this handler can handle the exception type; otherwise, false.</returns>
    bool CanHandle(Type exceptionType);

    /// <summary>
    /// Handles the exception and returns the status code and message.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="context">Additional context information (optional).</param>
    /// <returns>A tuple containing the HTTP status code and error message.</returns>
    (int statusCode, string message) Handle(Exception exception, Dictionary<string, object>? context = null);

    /// <summary>
    /// Gets the priority of this handler. Lower numbers have higher priority.
    /// Default handlers have priority 100, custom handlers should use lower numbers.
    /// </summary>
    int Priority { get; }
} 