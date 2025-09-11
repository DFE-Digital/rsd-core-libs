namespace GovUK.Dfe.CoreLibs.Security.TokenRefresh.Exceptions
{
    /// <summary>
    /// Represents errors that occur during token refresh operations.
    /// </summary>
    public class TokenRefreshException : Exception
    {
        /// <summary>
        /// Gets the OAuth 2.0 error code associated with this exception, if available.
        /// </summary>
        public string? ErrorCode { get; }

        /// <summary>
        /// Gets the HTTP status code associated with this exception, if available.
        /// </summary>
        public int? StatusCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenRefreshException"/> class.
        /// </summary>
        public TokenRefreshException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenRefreshException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TokenRefreshException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenRefreshException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TokenRefreshException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenRefreshException"/> class with a specified error message and OAuth 2.0 error code.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errorCode">The OAuth 2.0 error code.</param>
        public TokenRefreshException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenRefreshException"/> class with a specified error message, OAuth 2.0 error code, and HTTP status code.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errorCode">The OAuth 2.0 error code.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        public TokenRefreshException(string message, string errorCode, int statusCode) : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenRefreshException"/> class with a specified error message, OAuth 2.0 error code, and inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errorCode">The OAuth 2.0 error code.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TokenRefreshException(string message, string errorCode, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
