using AutoFixture;
using AutoFixture.AutoNSubstitute;
using GovUK.Dfe.CoreLibs.Security.Utils;

namespace GovUK.Dfe.CoreLibs.Security.Tests.CypressTests
{
    public class ParsedUserContextTests
    {
        private readonly IFixture _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

        [Fact(DisplayName = "FromHeaders returns null when name header is missing")]
        public void FromHeaders_ReturnsNull_WhenNameHeaderIsMissing()
        {
            // Arrange: Only a role header is provided.
            var headers = new[]
            {
                new KeyValuePair<string, string>("x-user-context-role-0", "Admin")
            };

            // Act
            var result = ParsedUserContext.FromHeaders(headers);

            // Assert
            Assert.Null(result);
        }

        [Fact(DisplayName = "FromHeaders returns null when role headers are missing")]
        public void FromHeaders_ReturnsNull_WhenRoleHeadersAreMissing()
        {
            // Arrange: Only the name header is provided.
            var headers = new[]
            {
                new KeyValuePair<string, string>("x-user-context-name", "Alice")
            };

            // Act
            var result = ParsedUserContext.FromHeaders(headers);

            // Assert
            Assert.Null(result);
        }

        [Fact(DisplayName = "FromHeaders returns null when name is whitespace")]
        public void FromHeaders_ReturnsNull_WhenNameIsWhitespace()
        {
            // Arrange: Name header is whitespace, and a role header exists.
            var headers = new[]
            {
                new KeyValuePair<string, string>("x-user-context-name", "   "),
                new KeyValuePair<string, string>("x-user-context-role-0", "Admin")
            };

            // Act
            var result = ParsedUserContext.FromHeaders(headers);

            // Assert
            Assert.Null(result);
        }

        [Fact(DisplayName = "FromHeaders returns a valid ParsedUserContext for valid headers")]
        public void FromHeaders_ReturnsParsedUserContext_ForValidHeaders()
        {
            // Arrange: Provide a valid name header and multiple role headers.
            var headers = new[]
            {
                new KeyValuePair<string, string>("x-user-context-name", "Alice"),
                new KeyValuePair<string, string>("x-user-context-role-0", "Admin"),
                new KeyValuePair<string, string>("x-user-context-role-1", "User")
            };

            // Act
            var result = ParsedUserContext.FromHeaders(headers);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Alice", result.Name);
            Assert.Equal(2, result.Roles.Count);
            Assert.Contains("Admin", result.Roles);
            Assert.Contains("User", result.Roles);
        }

        [Fact(DisplayName = "FromHeaders ignores case for name header")]
        public void FromHeaders_IgnoresCase_ForNameHeader()
        {
            // Arrange: Provide the name header in a different case.
            var headers = new[]
            {
                new KeyValuePair<string, string>("X-USER-CONTEXT-NAME", "Bob"),
                new KeyValuePair<string, string>("x-user-context-role-0", "Manager")
            };

            // Act
            var result = ParsedUserContext.FromHeaders(headers);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Bob", result.Name);
        }

        [Fact(DisplayName = "FromHeaders ignores case for role headers")]
        public void FromHeaders_IgnoresCase_ForRoleHeaders()
        {
            // Arrange: Provide a role header in a different case.
            var headers = new[]
            {
                new KeyValuePair<string, string>("x-user-context-name", "Charlie"),
                new KeyValuePair<string, string>("X-USER-CONTEXT-ROLE-0", "Editor")
            };

            // Act
            var result = ParsedUserContext.FromHeaders(headers);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Charlie", result.Name);
            Assert.Single(result.Roles);
            Assert.Contains("Editor", result.Roles);
        }
    }
}
