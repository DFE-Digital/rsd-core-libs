using AutoFixture.AutoNSubstitute;
using AutoFixture;
using Microsoft.AspNetCore.Http; 
using DfE.CoreLibs.Security.Antiforgery;

namespace DfE.CoreLibs.Security.Tests.Antiforgery
{
    public class CustomRequestCheckerTests
    {
        private readonly IFixture _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        private static readonly string HeaderKey = "x-custom-request";

        private static DefaultHttpContext CreateHttpContext(string headerValue)
        {
            var httpContext = new DefaultHttpContext(); 
            httpContext.Request.Headers[HeaderKey] = headerValue;
            return httpContext;
        }

        [Fact]
        public void IsCustomRequest_ReturnsFalse_WhenNoHeaderKey()
        {
            // Arrange 

            var httpContext = CreateHttpContext("ruby");
            var checker = new CustomRequestChecker();

            // Act
            var result = checker.IsValidRequest(httpContext, null);

            // Assert
            Assert.False(result);
        }
        [Fact]
        public void IsCustomRequest_ReturnsFalse_WhenHeaderKeyDoesNotMatched()
        {
            // Arrange 

            var httpContext = CreateHttpContext("ruby");
            var checker = new CustomRequestChecker();

            // Act
            var result = checker.IsValidRequest(httpContext, "x-header-key");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCustomRequest_ReturnsTrue_WhenHeaderKeyMatches()
        {
            // Arrange 

            var httpContext = CreateHttpContext("ruby");
            var checker = new CustomRequestChecker();

            // Act
            var result = checker.IsValidRequest(httpContext, HeaderKey);

            // Assert
            Assert.True(result);
        } 
    }
}
