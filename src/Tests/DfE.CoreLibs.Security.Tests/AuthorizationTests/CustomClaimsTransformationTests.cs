using DfE.CoreLibs.Security.Authorization;
using DfE.CoreLibs.Security.Interfaces;
using NSubstitute;
using System.Security.Claims;

namespace DfE.CoreLibs.Security.Tests.AuthorizationTests
{
    public class CustomClaimsTransformationTests
    {
        [Fact]
        public async Task TransformAsync_ShouldAddClaimsFromAllProviders()
        {
            // Arrange
            var claimProvider1 = Substitute.For<ICustomClaimProvider>();
            claimProvider1.GetClaimsAsync(Arg.Any<ClaimsPrincipal>()).Returns(new List<Claim> { new Claim("Type1", "Value1") });

            var claimProvider2 = Substitute.For<ICustomClaimProvider>();
            claimProvider2.GetClaimsAsync(Arg.Any<ClaimsPrincipal>()).Returns(new List<Claim> { new Claim("Type2", "Value2") });

            var transformation = new CustomClaimsTransformation(new List<ICustomClaimProvider> { claimProvider1, claimProvider2 });
            var principal = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var transformedPrincipal = await transformation.TransformAsync(principal);

            // Assert
            Assert.Contains(transformedPrincipal.Claims, c => c.Type == "Type1" && c.Value == "Value1");
            Assert.Contains(transformedPrincipal.Claims, c => c.Type == "Type2" && c.Value == "Value2");
        }

        [Fact]
        public async Task TransformAsync_ShouldNotAddDuplicateClaims()
        {
            // Arrange
            var claimProvider = Substitute.For<ICustomClaimProvider>();
            claimProvider.GetClaimsAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(new List<Claim> { new Claim("Type1", "Value1") });

            var transformation = new CustomClaimsTransformation(new List<ICustomClaimProvider> { claimProvider });
            var identity = new ClaimsIdentity(new List<Claim> { new Claim("Type1", "Value1") });
            var principal = new ClaimsPrincipal(identity);

            // Act
            var transformedPrincipal = await transformation.TransformAsync(principal);

            // Assert
            Assert.Single(transformedPrincipal.Claims, c => c.Type == "Type1" && c.Value == "Value1");
        }
    }
}
