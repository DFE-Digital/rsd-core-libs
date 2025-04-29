namespace DfE.CoreLibs.Security.Utils
{
    /// <summary>
    /// Represents user context information parsed from HTTP headers, including a username and roles
    /// </summary>
    /// <param name="Name">
    /// The user's name, extracted from the "x-user-context-name" header.
    /// </param>
    /// <param name="AdId">
    /// The user's Active Directory Id, extracted from the "x-user-ad-id" header.
    /// </param>
    /// <param name="Roles">
    /// A collection of roles extracted from headers starting with "x-user-context-role-".
    /// </param>
    public record ParsedUserContext(
        string Name,
        string AdId,
        IReadOnlyList<string>? Roles)
    {
        public const string NameHeaderKey = "x-user-context-name";
        public const string RoleHeaderKeyPrefix = "x-user-context-role-";
        private const string ActiveDirectoryKey = "x-user-ad-id";

        /// <summary>
        /// Creates a new <see cref="ParsedUserContext"/> by extracting user information 
        /// from the specified collection of header key/value pairs.
        /// </summary>
        /// <param name="headers">
        /// A collection of key/value headers that may contain user context information.
        /// </param>
        /// <returns>
        /// A new <see cref="ParsedUserContext"/> if valid user info is found (i.e., a name 
        /// and at least one role); otherwise <c>null</c>.
        /// </returns>
        public static ParsedUserContext? FromHeaders(KeyValuePair<string, string>[]? headers)
        {
            if (headers == null)
                return null;

            // Extract name from "x-user-context-name"
            var name = headers.FirstOrDefault(x => x.Key.Equals(NameHeaderKey, StringComparison.InvariantCultureIgnoreCase)).Value;

            // Extract AdId from "x-user-ad-id"
            var adId = headers.FirstOrDefault(x => x.Key.Equals(ActiveDirectoryKey, StringComparison.InvariantCultureIgnoreCase)).Value;

            // Extract roles from any header starting with "x-user-context-role-"
            var roles = headers
                .Where(h => h.Key.StartsWith(RoleHeaderKeyPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(h => h.Value)
                .ToArray();

            if (string.IsNullOrWhiteSpace(name))
                return null;

            // If no adId, then roles must be provided
            if (string.IsNullOrWhiteSpace(adId) && roles.Length == 0)
                return null;

            return new ParsedUserContext(name, adId, roles);
        }
    }
}
