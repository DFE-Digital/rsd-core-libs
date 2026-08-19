namespace GovUK.Dfe.CoreLibs.Http.Logging;

/// <summary>
/// Canonical structured-log property names for cross-service multi-tenant tracing in Application Insights.
/// Product-specific keys (e.g. form template ids) belong in the consuming application, not this package.
/// </summary>
public static class LogContextKeys
{
    public const string CorrelationId = "CorrelationId";
    public const string ErrorId = "ErrorId";
    public const string TenantId = "TenantId";
    public const string TenantName = "TenantName";
    public const string UserId = "UserId";
    public const string UserEmail = "UserEmail";
    public const string ServiceName = "ServiceName";
}
