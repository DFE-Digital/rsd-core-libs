using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Http.Models;

/// <summary>
/// Standardized exception response model for consistent error handling across applications.
/// </summary>
public class ExceptionResponse
{
    /// <summary>
    /// Unique 6-digit identifier for tracking the exception in logs and monitoring.
    /// </summary>
    [JsonPropertyName("errorId")]
    public string ErrorId { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code for the error.
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detailed error information (only included in development).
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }

    /// <summary>
    /// Exception type name for debugging purposes.
    /// </summary>
    [JsonPropertyName("exceptionType")]
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the exception occurred.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for request tracing.
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Additional context information (optional).
    /// </summary>
    [JsonPropertyName("context")]
    public Dictionary<string, object>? Context { get; set; }
} 
