using GovUK.Dfe.CoreLibs.Http.Interfaces;
using GovUK.Dfe.CoreLibs.Http.Models;

namespace GovUK.Dfe.CoreLibs.Http.Logging;

/// <summary>
/// Applies ambient request telemetry to exception responses and log context dictionaries.
/// </summary>
public static class RequestTelemetryEnrichment
{
    public static void ApplyToExceptionResponse(ExceptionResponse response, IRequestTelemetryContext? telemetry)
    {
        if (telemetry is null)
            return;

        response.TenantId ??= telemetry.TenantId;
        response.TenantName ??= telemetry.TenantName;
        response.UserEmail ??= telemetry.UserEmail;
        response.CorrelationId ??= telemetry.CorrelationId;

        response.Context ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        MergeContext(response.Context, LogContextKeys.UserId, telemetry.UserId);
        MergeContext(response.Context, LogContextKeys.ServiceName, telemetry.ServiceName);
    }

    private static void MergeContext(Dictionary<string, object> context, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !context.ContainsKey(key))
            context[key] = value;
    }
}
