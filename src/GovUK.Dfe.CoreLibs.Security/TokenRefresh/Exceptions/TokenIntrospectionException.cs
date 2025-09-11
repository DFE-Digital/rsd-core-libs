namespace GovUK.Dfe.CoreLibs.Security.TokenRefresh.Exceptions
{
    /// <summary>
    /// Represents errors that occur during token introspection operations.
    /// </summary>
    public class TokenIntrospectionException : Exception
    {
        /// <summary>
        /// Gets the HTTP status code associated with this exception, if available.
        /// </summary>
        public int? StatusCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenIntrospectionException"/> class.
        /// </summary>
        public TokenIntrospectionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenIntrospectionException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TokenIntrospectionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenIntrospectionException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TokenIntrospectionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenIntrospectionException"/> class with a specified error message and HTTP status code.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        public TokenIntrospectionException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenIntrospectionException"/> class with a specified error message, HTTP status code, and inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TokenIntrospectionException(string message, int statusCode, Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
