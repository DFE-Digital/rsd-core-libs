namespace DfE.CoreLibs.Security.Utils
{
    /// <summary>
    /// Represents user context information parsed from HTTP headers, including a username and roles
    /// </summary>
    /// <param name="Name">
    /// The user's name, extracted from the "x-user-context-name" header.
    /// </param>
    /// <param name="Roles">
    /// A collection of roles extracted from headers starting with "x-user-context-role-".
    /// </param>
    public record ParsedUserContext(
        string Name,
        IReadOnlyList<string> Roles)
    {
        public const string NameHeaderKey = "x-user-context-name";
        public const string RoleHeaderKeyPrefix = "x-user-context-role-";

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
        public static ParsedUserContext? FromHeaders(IEnumerable<KeyValuePair<string, string>> headers)
        {
            // Extract name from "x-user-context-name"
            var name = headers.FirstOrDefault(x => x.Key.Equals(NameHeaderKey, StringComparison.InvariantCultureIgnoreCase)).Value;

            // Extract roles from any header starting with "x-user-context-role-"
            var roles = headers
                .Where(h => h.Key.StartsWith(RoleHeaderKeyPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(h => h.Value)
                .ToArray();

            // If missing name/roles, return null
            if (string.IsNullOrWhiteSpace(name) || roles.Length == 0)
                return null;

            return new ParsedUserContext(name, roles);
        }
    }
}
