using GovUK.Dfe.CoreLibs.Http.Interfaces;
using GovUK.Dfe.CoreLibs.Http.Models;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.CoreLibs.Http.Configuration;

/// <summary>
/// Configuration options for the global exception handler middleware.
/// </summary>
public class ExceptionHandlerOptions
{
    /// <summary>
    /// Whether to include detailed exception information in responses (default: false).
    /// Should be false in production environments.
    /// </summary>
    public bool IncludeDetails { get; set; } = false;

    /// <summary>
    /// Whether to log exceptions automatically (default: true).
    /// </summary>
    public bool LogExceptions { get; set; } = true;

    /// <summary>
    /// Log level for exceptions mapped to a client error (4xx) response (default: null).
    /// When set, these are logged as a single line at this level without the exception object,
    /// so telemetry sinks record a trace rather than a full exception with stack trace.
    /// When null, every handled exception is logged at Error with the exception attached.
    /// Server errors (5xx) are always logged at Error with the exception attached.
    /// </summary>
    public LogLevel? ClientErrorLogLevel { get; set; } = null;

    /// <summary>
    /// Exception types that should be ignored (not handled by the middleware).
    /// </summary>
    public HashSet<Type> IgnoredExceptionTypes { get; set; } = new();

    /// <summary>
    /// Custom error message for unhandled exceptions (default: "An unexpected error occurred").
    /// </summary>
    public string DefaultErrorMessage { get; set; } = "An unexpected error occurred";

    /// <summary>
    /// Whether to include correlation ID in error responses (default: true).
    /// </summary>
    public bool IncludeCorrelationId { get; set; } = true;

    /// <summary>
    /// Custom function to generate error IDs (default: 6-digit random number).
    /// If not provided, a default 6-digit random number generator will be used.
    /// </summary>
    public Func<string>? ErrorIdGenerator { get; set; } = null;

    /// <summary>
    /// Custom exception handlers registered with the middleware.
    /// These handlers will be called before the default handler.
    /// </summary>
    public List<ICustomExceptionHandler> CustomHandlers { get; set; } = new();

    /// <summary>
    /// Shared post-processing action that will be executed after any handler processes an exception.
    /// This allows for common logic like logging, metrics, or notifications without duplicating code.
    /// </summary>
    public Action<Exception, ExceptionResponse>? SharedPostProcessingAction { get; set; } = null;
} 
