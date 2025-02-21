using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace DfE.CoreLibs.Security.Cypress
{
    /// <inheritdoc />
    public class CypressRequestChecker(IHostEnvironment env, IConfiguration config) : ICypressRequestChecker
    {
        /// <inheritdoc />
        public bool IsCypressRequest(HttpContext httpContext)
        {
            // Read config and environment 
            var secret = config["CypressTestSecret"];
            var environmentName = env.EnvironmentName;

            // Read headers
            var authHeaderValue = httpContext.Request.Headers[HeaderNames.Authorization]
                .ToString()
                .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

            var userContextHeaderValue = httpContext.Request.Headers["x-cypress-user"].ToString();

            // Must match "cypressUser"
            if (!userContextHeaderValue.Equals("cypressUser", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Only allow Dev/Staging
            if (!environmentName.Equals("Development", StringComparison.OrdinalIgnoreCase) &&
                !environmentName.Equals("Staging", StringComparison.OrdinalIgnoreCase) &&
                !environmentName.Equals("Test", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Compare secrets
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(authHeaderValue))
            {
                return false;
            }

            return authHeaderValue.Equals(secret, StringComparison.Ordinal);
        }
    }
}
