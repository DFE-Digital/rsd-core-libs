using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DfE.CoreLibs.Security.Antiforgery
{
    /// <inheritdoc />
    public class CustomRequestChecker()
        : ICustomRequestChecker
    {
        public bool IsValidRequest(HttpContext httpContext, string? headerKey)
        {
            if (string.IsNullOrWhiteSpace(headerKey))
            {
                return false;
            }

            var requestHeader = httpContext.Request.Headers[headerKey];
            return string.IsNullOrWhiteSpace(requestHeader);
        }
    }
}