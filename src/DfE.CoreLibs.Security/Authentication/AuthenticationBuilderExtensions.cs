using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace DfE.CoreLibs.Security.Authentication
{
    public static class AuthenticationBuilderExtensions
    {
        /// <summary>
        /// Adds a JwtBearer handler under the given scheme name, wires up the
        /// standard options via configureOptions, and lets you override
        /// the OnMessageReceived event.
        /// </summary>
        public static AuthenticationBuilder AddJwtBearerScheme(
            this AuthenticationBuilder builder,
            string scheme,
            Action<JwtBearerOptions> configureOptions,
            Func<MessageReceivedContext, Task>? onMessageReceived = null)
        {
            return builder.AddJwtBearer(scheme, options =>
            {
                configureOptions(options);

                options.Events ??= new JwtBearerEvents();

                if (onMessageReceived != null)
                {
                    options.Events.OnMessageReceived = onMessageReceived;
                }
            });
        }
    }
}
