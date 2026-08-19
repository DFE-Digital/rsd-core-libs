using GovUK.Dfe.CoreLibs.Http.Interfaces;
using GovUK.Dfe.CoreLibs.Http.Logging;

namespace GovUK.Dfe.CoreLibs.Http.Middlewares.RequestTelemetry;

/// <inheritdoc />
public sealed class RequestTelemetryContext : IRequestTelemetryContext
{
    public string? CorrelationId { get; set; }
    public string? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? ServiceName { get; set; }

    public IReadOnlyDictionary<string, object> ToScopeDictionary()
    {
        var scope = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        AddIfPresent(scope, LogContextKeys.CorrelationId, CorrelationId);
        AddIfPresent(scope, LogContextKeys.TenantId, TenantId);
        AddIfPresent(scope, LogContextKeys.TenantName, TenantName);
        AddIfPresent(scope, LogContextKeys.UserId, UserId);
        AddIfPresent(scope, LogContextKeys.UserEmail, UserEmail);
        AddIfPresent(scope, LogContextKeys.ServiceName, ServiceName);

        return scope;
    }

    private static void AddIfPresent(IDictionary<string, object> scope, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            scope[key] = value;
    }
}
