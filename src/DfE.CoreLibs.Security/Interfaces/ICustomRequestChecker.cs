using Microsoft.AspNetCore.Http; 

namespace DfE.CoreLibs.Security.Interfaces
{ 
    /// <summary>
    /// Represents a service that detects whether an incoming HTTP request 
    /// is a valid request (based on environment, headers, etc.).
    /// </summary>
    public interface ICustomRequestChecker
    {
        /// <summary>
        /// Determines whether the specified <see cref="HttpContext"/> 
        /// represents a valid custom request.
        /// </summary>
        /// <param name="httpContext">
        /// The current HTTP request context from which to read headers, config values, etc.
        /// </param>
        /// <param name="headerKey">
        /// Header key to check in the request headers to determine validity.
        /// </param>
        /// <param name="headerValue">
        /// Header Value to check in the request headers to determine validity.
        /// </param>
        /// <returns>
        /// <c>true</c> if the request is recognized as a valid request; 
        /// otherwise, <c>false</c>.
        /// </returns>
        bool IsValidRequest(HttpContext httpContext, string? headerKey, string? headerValue);
    }
}
