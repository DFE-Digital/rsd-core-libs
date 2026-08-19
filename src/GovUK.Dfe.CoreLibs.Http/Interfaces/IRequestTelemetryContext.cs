namespace GovUK.Dfe.CoreLibs.Http.Interfaces;

/// <summary>
/// Ambient per-request telemetry bag populated by host middleware (tenant, user, service).
/// Read by the global exception handler and logging helpers.
/// Product-specific fields should live in the consuming application and be added via log scopes / ExceptionResponse.Context.
/// </summary>
public interface IRequestTelemetryContext
{
    string? CorrelationId { get; set; }
    string? TenantId { get; set; }
    string? TenantName { get; set; }
    string? UserId { get; set; }
    string? UserEmail { get; set; }
    string? ServiceName { get; set; }

    /// <summary>
    /// Returns non-null key/value pairs suitable for ILogger.BeginScope.
    /// </summary>
    IReadOnlyDictionary<string, object> ToScopeDictionary();
}
