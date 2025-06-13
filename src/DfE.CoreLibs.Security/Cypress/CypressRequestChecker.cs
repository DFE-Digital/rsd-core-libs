using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace DfE.CoreLibs.Security.Cypress
{
    /// <inheritdoc />
    public class CypressRequestChecker(IHostEnvironment env, IConfiguration config)
        : ICustomRequestChecker
    {
        private const string CypressUserHeaderKey = "x-cypress-user";
        private const string ExpectedCypressUser = "cypressUser";

        public bool IsValidRequest(HttpContext httpContext)
        {
            var userHeader = httpContext.Request.Headers[CypressUserHeaderKey].ToString();
            if (!string.Equals(userHeader, ExpectedCypressUser, StringComparison.OrdinalIgnoreCase))
                return false;

            // Only allow in Dev, Staging or Test
            if (!(env.IsDevelopment()
                  || env.IsStaging()
                  || env.IsEnvironment("Test")))
            {
                return false;
            }

            var secret = config["CypressTestSecret"];
            var authHdr = httpContext.Request.Headers[HeaderNames.Authorization]
                .ToString()
                .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(authHdr))
                return false;

            return string.Equals(authHdr, secret, StringComparison.Ordinal);
        }
    }
}