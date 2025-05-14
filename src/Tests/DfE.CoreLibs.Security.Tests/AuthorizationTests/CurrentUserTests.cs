using DfE.CoreLibs.Security.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DfE.CoreLibs.Security.Tests.AuthorizationTests
{
    public class CurrentUserTests
    {
        [Fact]
        public void Ctor_Throws_WhenNoHttpContext()
        {
            var accessor = new HttpContextAccessor(); // HttpContext is null
            Assert.Throws<InvalidOperationException>(() => new CurrentUser(accessor));
        }

        [Fact]
        public void Id_And_Name_ArePulledFromHttpContextUser()
        {
            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-id-123"),
                new Claim(ClaimTypes.Name, "Alice")
            }, "test"));

            var accessor = new HttpContextAccessor { HttpContext = ctx };
            var current = new CurrentUser(accessor);

            Assert.Equal("user-id-123", current.Id);
            Assert.Equal("Alice", current.Name);
        }

        [Fact]
        public void HasPermission_DelegatesToClaimsPrincipalExtension()
        {
            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("permission", "resX:DoIt")
            }));

            var accessor = new HttpContextAccessor { HttpContext = ctx };
            var current = new CurrentUser(accessor);

            Assert.True(current.HasPermission("resX", "DoIt"));
            Assert.False(current.HasPermission("resX", "Other"));
        }
    }
}
