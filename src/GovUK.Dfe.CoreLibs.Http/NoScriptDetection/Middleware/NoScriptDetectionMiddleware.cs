using GovUK.Dfe.CoreLibs.Http.NoScriptDetection.Internal;
using GovUK.Dfe.CoreLibs.Http.NoScriptDetection.Pixel;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace GovUK.Dfe.CoreLibs.Http.NoScriptDetection.Middleware
{
    internal sealed class NoScriptDetectionMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(
            HttpContext context,
            TelemetryClient telemetryClient,
            INoScriptPixelProvider pixelProvider)
        {
            if (context.Request.Path == Constants.EndpointPath)
            {
                telemetryClient.TrackEvent(Constants.TelemetryEventName);

                context.Response.ContentType = "image/png";
                context.Response.GetTypedHeaders().CacheControl =
                    new CacheControlHeaderValue
                    {
                        NoStore = true,
                        NoCache = true,
                        MustRevalidate = true
                    };

                await context.Response.Body.WriteAsync(
                    pixelProvider.GetPixel(),
                    context.RequestAborted);

                return;
            }

            await _next(context);
        }
    }
}
