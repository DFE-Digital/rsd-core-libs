using GovUK.Dfe.CoreLibs.Http.NoScriptDetection.Pixel;
using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.Http.NoScriptDetection
{
    public static class NoScriptDetectionServiceCollectionExtensions
    {
        public static IServiceCollection AddNoScriptDetection(
            this IServiceCollection services)
        {
            services.AddSingleton<INoScriptPixelProvider, TransparentPixelProvider>();
            return services;
        }
    }
}
