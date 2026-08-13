namespace GovUK.Dfe.CoreLibs.Http.Interfaces;

/// <summary>
/// Ambient per-request telemetry bag populated by host middleware (tenant, user, template, etc.).
/// Read by the global exception handler and logging helpers.
/// </summary>
public interface IRequestTelemetryContext
{
    string? CorrelationId { get; set; }
    string? TenantId { get; set; }
    string? TenantName { get; set; }
    string? UserId { get; set; }
    string? UserEmail { get; set; }
    string? TemplateId { get; set; }
    string? ApplicationId { get; set; }
    string? ApplicationReference { get; set; }
    string? ServiceName { get; set; }

    /// <summary>
    /// Returns non-null key/value pairs suitable for ILogger.BeginScope.
    /// </summary>
    IReadOnlyDictionary<string, object> ToScopeDictionary();
}
