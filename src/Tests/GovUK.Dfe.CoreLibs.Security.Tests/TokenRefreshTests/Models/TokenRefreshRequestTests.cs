using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models;

namespace GovUK.Dfe.CoreLibs.Security.Tests.TokenRefreshTests.Models
{
    public class TokenRefreshRequestTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var request = new TokenRefreshRequest();

            // Assert
            Assert.NotNull(request.AdditionalParameters);
            Assert.Empty(request.AdditionalParameters);
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var request = new TokenRefreshRequest();
            const string refreshToken = "test_refresh_token";
            const string clientId = "test_client_id";
            const string clientSecret = "test_client_secret";
            const string scope = "openid profile";
            const string tokenEndpoint = "https://example.com/token";

            // Act
            request.RefreshToken = refreshToken;
            request.ClientId = clientId;
            request.ClientSecret = clientSecret;
            request.Scope = scope;
            request.TokenEndpoint = tokenEndpoint;

            // Assert
            Assert.Equal(refreshToken, request.RefreshToken);
            Assert.Equal(clientId, request.ClientId);
            Assert.Equal(clientSecret, request.ClientSecret);
            Assert.Equal(scope, request.Scope);
            Assert.Equal(tokenEndpoint, request.TokenEndpoint);
        }

        [Fact]
        public void AdditionalParameters_CanBeModified()
        {
            // Arrange
            var request = new TokenRefreshRequest();
            const string key = "custom_param";
            const string value = "custom_value";

            // Act
            request.AdditionalParameters[key] = value;

            // Assert
            Assert.True(request.AdditionalParameters.ContainsKey(key));
            Assert.Equal(value, request.AdditionalParameters[key]);
        }

        [Fact]
        public void AdditionalParameters_CanBeReplaced()
        {
            // Arrange
            var request = new TokenRefreshRequest();
            var newParameters = new Dictionary<string, string>
            {
                { "param1", "value1" },
                { "param2", "value2" }
            };

            // Act
            request.AdditionalParameters = newParameters;

            // Assert
            Assert.Equal(2, request.AdditionalParameters.Count);
            Assert.Equal("value1", request.AdditionalParameters["param1"]);
            Assert.Equal("value2", request.AdditionalParameters["param2"]);
        }
    }
}
