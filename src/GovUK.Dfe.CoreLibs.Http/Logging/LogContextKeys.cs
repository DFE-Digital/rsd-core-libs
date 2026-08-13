namespace GovUK.Dfe.CoreLibs.Http.Logging;

/// <summary>
/// Canonical structured-log property names for cross-service SaaS tracing in Application Insights.
/// Use these keys in BeginScope dictionaries and Serilog enrichers so Web and API queries align.
/// </summary>
public static class LogContextKeys
{
    public const string CorrelationId = "CorrelationId";
    public const string ErrorId = "ErrorId";
    public const string TenantId = "TenantId";
    public const string TenantName = "TenantName";
    public const string UserId = "UserId";
    public const string UserEmail = "UserEmail";
    public const string TemplateId = "TemplateId";
    public const string ApplicationId = "ApplicationId";
    public const string ApplicationReference = "ApplicationReference";
    public const string ServiceName = "ServiceName";
}
