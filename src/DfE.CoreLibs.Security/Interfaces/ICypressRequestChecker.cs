using Microsoft.AspNetCore.Http;

namespace DfE.CoreLibs.Security.Interfaces
{
    /// <summary>
    /// Represents a service that detects whether an incoming HTTP request 
    /// is a valid "Cypress" request (based on environment, headers, etc.).
    /// </summary>
    public interface ICypressRequestChecker
    {
        /// <summary>
        /// Determines whether the specified <see cref="HttpContext"/> 
        /// represents a valid Cypress request.
        /// </summary>
        /// <param name="httpContext">
        /// The current HTTP request context from which to read headers, config values, etc.
        /// </param>
        /// <returns>
        /// <c>true</c> if the request is recognized as a valid Cypress request; 
        /// otherwise, <c>false</c>.
        /// </returns>
        bool IsCypressRequest(HttpContext httpContext);
    }
}
