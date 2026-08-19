using GovUK.Dfe.CoreLibs.Http.Interfaces;
using GovUK.Dfe.CoreLibs.Http.Middlewares.CorrelationId;
using GovUK.Dfe.CoreLibs.Http.Middlewares.RequestTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.Http.Extensions;

/// <summary>
/// Registers correlation-id middleware and request telemetry context for SaaS tracing.
/// </summary>
public static class CorrelationIdExtensions
{
    /// <summary>
    /// Registers scoped <see cref="ICorrelationContext"/> and <see cref="IRequestTelemetryContext"/>.
    /// </summary>
    public static IServiceCollection AddCorrelationId(this IServiceCollection services)
    {
        services.AddScoped<ICorrelationContext, CorrelationContext>();
        services.AddScoped<IRequestTelemetryContext, RequestTelemetryContext>();
        return services;
    }

    /// <summary>
    /// Ensures every request has an <c>x-correlationId</c> header and populates log scopes.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
