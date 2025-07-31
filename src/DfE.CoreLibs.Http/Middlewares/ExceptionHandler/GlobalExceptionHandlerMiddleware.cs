using DfE.CoreLibs.Http.Configuration;
using DfE.CoreLibs.Http.Handlers;
using DfE.CoreLibs.Http.Interfaces;
using DfE.CoreLibs.Http.Models;
using DfE.CoreLibs.Http.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DfE.CoreLibs.Http.Middlewares.ExceptionHandler;

/// <summary>
/// Global exception handler middleware that catches all unhandled exceptions and formats them consistently.
/// Provides standardized error responses with unique error IDs for tracking in Application Insights.
/// Supports custom exception handlers for extensible error handling.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly ExceptionHandlerOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<ICustomExceptionHandler> _handlers;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        ExceptionHandlerOptions options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Initialize handlers with custom handlers first, then default handler
        _handlers = new List<ICustomExceptionHandler>();
        
        // Add custom handlers from options
        _handlers.AddRange(_options.CustomHandlers);
        
        // Add default handler
        _handlers.Add(new DefaultExceptionHandler());
        
        // Sort by priority (lower numbers have higher priority)
        _handlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Check if this exception type should be ignored
        if (_options.IgnoredExceptionTypes.Contains(exception.GetType()))
        {
            throw exception; // Re-throw ignored exceptions
        }

        // Generate unique error ID using custom generator or default
        var errorId = _options.ErrorIdGenerator?.Invoke() ?? ErrorIdGenerator.GenerateDefault();

        // Get correlation ID if available
        string? correlationId = null;
        if (_options.IncludeCorrelationId)
        {
            var correlationContext = context.RequestServices.GetService<ICorrelationContext>();
            correlationId = correlationContext?.CorrelationId.ToString();
        }

        // Create context dictionary for handlers to use
        var handlerContext = new Dictionary<string, object>();

        // Get exception response from custom handlers
        var exceptionResponse = GetExceptionResponse(exception, handlerContext, context.RequestServices);

        // Create final error response with middleware-specific properties
        var errorResponse = new ExceptionResponse
        {
            ErrorId = errorId,
            StatusCode = exceptionResponse.StatusCode,
            Message = exceptionResponse.Message,
            ExceptionType = exceptionResponse.ExceptionType ?? exception.GetType().Name,
            Timestamp = DateTime.UtcNow,
            CorrelationId = correlationId,
            Details = _options.IncludeDetails ? exception.ToString() : exceptionResponse.Details,
            Context = exceptionResponse.Context ?? (handlerContext.Any() ? handlerContext : null)
        };

        // Execute shared post-processing action if configured
        try
        {
            _options.SharedPostProcessingAction?.Invoke(exception, errorResponse);
        }
        catch (Exception postProcessingException)
        {
            _logger.LogError(postProcessingException, "Error during shared post-processing action for exception with ErrorId: {ErrorId}", errorId);
        }

        // Log the exception
        if (_options.LogExceptions)
        {
            LogException(exception, errorId, correlationId);
        }

        // Set response
        context.Response.StatusCode = errorResponse.StatusCode;
        context.Response.ContentType = "application/json";

        // Serialize and write response
        var jsonResponse = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    private ExceptionResponse GetExceptionResponse(Exception exception, Dictionary<string, object> context, IServiceProvider serviceProvider)
    {
        var exceptionType = exception.GetType();

        // Try custom handlers from service container first
        var serviceHandlers = serviceProvider.GetServices<ICustomExceptionHandler>();
        foreach (var handler in serviceHandlers)
        {
            if (handler.CanHandle(exceptionType))
            {
                return handler.Handle(exception, context);
            }
        }

        // Try handlers from options
        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(exceptionType))
            {
                return handler.Handle(exception, context);
            }
        }

        // Final fallback
        return new ExceptionResponse
        {
            StatusCode = 500,
            Message = _options.DefaultErrorMessage,
            ExceptionType = exception.GetType().Name
        };
    }

    private void LogException(Exception exception, string errorId, string? correlationId)
    {
        var logMessage = "Exception occurred with ErrorId: {ErrorId}";
        var logArgs = new object[] { errorId };

        if (!string.IsNullOrEmpty(correlationId))
        {
            logMessage += ", CorrelationId: {CorrelationId}";
            logArgs = logArgs.Concat(new object[] { correlationId }).ToArray();
        }

        _logger.LogError(exception, logMessage, logArgs);
    }
} 