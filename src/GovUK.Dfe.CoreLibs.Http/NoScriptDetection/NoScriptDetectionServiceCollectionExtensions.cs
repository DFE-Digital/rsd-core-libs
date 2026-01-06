using GovUK.Dfe.CoreLibs.Http.NoScriptDetection.Pixel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
