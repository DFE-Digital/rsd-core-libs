using GovUK.Dfe.CoreLibs.Security.Extensions;
using System.Security.Claims;

namespace GovUK.Dfe.CoreLibs.Security.Tests.AuthorizationTests
{
    public class PermissionExtensionsTests
    {
        [Fact]
        public void HasPermission_ReturnsTrue_WhenClaimMatchesDefaultType()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(PermissionExtensions.DefaultPermissionClaimType, "res1:Read")
            }));

            Assert.True(user.HasPermission("res1", "Read"));
        }

        [Fact]
        public void HasPermission_ReturnsFalse_WhenNoMatchingClaim()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(PermissionExtensions.DefaultPermissionClaimType, "res1:Write")
            }));

            Assert.False(user.HasPermission("res1", "Read"));
        }

        [Fact]
        public void HasPermission_Throws_OnNullUserOrEmptyArgs()
        {
            var user = new ClaimsPrincipal();
            Assert.Throws<ArgumentNullException>(() => PermissionExtensions.HasPermission(null!, "r", "a"));
            Assert.Throws<ArgumentException>(() => user.HasPermission("", "a"));
            Assert.Throws<ArgumentException>(() => user.HasPermission("r", ""));
        }

        [Fact]
        public void HasPermission_RespectsCustomClaimType()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("permX", "foo:Bar")
            }));

            Assert.True(user.HasPermission("foo", "Bar", "permX"));
            Assert.False(user.HasPermission("foo", "Bar")); // default type is "permission"
        }
    }
}
