using GovUK.Dfe.CoreLibs.Http.Interfaces;
using GovUK.Dfe.CoreLibs.Http.Logging;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace GovUK.Dfe.CoreLibs.Http.Middlewares.CorrelationId;

/// <summary>
/// Middleware that checks incoming requests for a correlation id header. If not found then a new value is created.
/// Saves the value in <see cref="ICorrelationContext"/> and <see cref="IRequestTelemetryContext"/>.
/// Header used in requests is 'x-correlationId'.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task Invoke(
        HttpContext httpContext,
        ICorrelationContext correlationContext,
        IRequestTelemetryContext? requestTelemetry = null)
    {
        Guid thisCorrelationId;

        if (httpContext.Request.Headers.ContainsKey(Keys.HeaderKey)
            && !string.IsNullOrWhiteSpace(httpContext.Request.Headers[Keys.HeaderKey]))
        {
            if (!Guid.TryParse(httpContext.Request.Headers[Keys.HeaderKey], out thisCorrelationId))
            {
                thisCorrelationId = Guid.NewGuid();
                _logger.LogDebug(
                    "x-correlationId header could not be parsed as GUID; generated {CorrelationId}",
                    thisCorrelationId);
            }
        }
        else
        {
            thisCorrelationId = Guid.NewGuid();
        }

        if (thisCorrelationId == Guid.Empty)
        {
            var result = new
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = $"Bad Request. {Keys.HeaderKey} header cannot be an empty GUID"
            };

            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            httpContext.Response.ContentType = "text/json";
            return httpContext.Response.WriteAsync(result.ToString());
        }

        httpContext.Request.Headers[Keys.HeaderKey] = thisCorrelationId.ToString();
        correlationContext.SetContext(thisCorrelationId);
        httpContext.Response.Headers[Keys.HeaderKey] = thisCorrelationId.ToString();

        var correlationIdString = thisCorrelationId.ToString();
        if (requestTelemetry is not null)
            requestTelemetry.CorrelationId = correlationIdString;

        var scope = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            [LogContextKeys.CorrelationId] = correlationIdString
        };

        using (_logger.BeginScope(scope))
        {
            return _next(httpContext);
        }
    }
}
