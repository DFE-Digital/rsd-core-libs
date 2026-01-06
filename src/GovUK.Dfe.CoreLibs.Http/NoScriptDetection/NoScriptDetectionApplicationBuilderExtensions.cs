using GovUK.Dfe.CoreLibs.Http.NoScriptDetection.Middleware;
using Microsoft.AspNetCore.Builder;

namespace GovUK.Dfe.CoreLibs.Http.NoScriptDetection
{
    public static class NoScriptDetectionApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseNoScriptDetection(
            this IApplicationBuilder app)
        {
            return app.UseMiddleware<NoScriptDetectionMiddleware>();
        }
    }
}
