using DfE.CoreLibs.Security.Enums;
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
        /// <returns>
        /// <c>true</c> if the request is recognized as a valid request; 
        /// otherwise, <c>false</c>.
        /// </returns>
        bool IsValidRequest(HttpContext httpContext);

        /// <summary>
        /// Gets the operator type that defines how multiple
        /// </summary>
        OperatorType Operator { get; }
    }
}
