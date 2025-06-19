using DfE.CoreLibs.Security.Configurations;
using DfE.CoreLibs.Security.OpenIdConnect;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DfE.CoreLibs.Security.Tests.OpenIdConnectTests
{
    public class ExternalIdentityValidatorTests
    {
        private readonly OpenIdConnectOptions _opts = new()
        {
            Issuer = "https://idp.example.com/",
            DiscoveryEndpoint = "https://idp.example.com/.well-known/openid-configuration"
        };

        [Fact]
        public void Ctor_NullOptions_Throws()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            Assert.Throws<ArgumentNullException>(() =>
                new ExternalIdentityValidator(null!, httpClientFactory));
        }

        [Fact]
        public void Ctor_NullHttpClientFactory_Throws()
        {
            var options = Options.Create(_opts);
            Assert.Throws<ArgumentNullException>(() =>
                new ExternalIdentityValidator(options, null!));
        }

        [Fact]
        public void Ctor_ValidArgs_DoesNotThrow()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>())
                             .Returns(new HttpClient());

            var options = Options.Create(_opts);
            var validator = new ExternalIdentityValidator(options, httpClientFactory);
            Assert.NotNull(validator);
        }

        [Fact]
        public async Task ValidateIdTokenAsync_NullOrEmpty_ThrowsArgumentNull()
        {
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>())
                             .Returns(new HttpClient());

            var validator = new ExternalIdentityValidator(
                Options.Create(_opts),
                httpClientFactory);

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => validator.ValidateIdTokenAsync(null!));

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => validator.ValidateIdTokenAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => validator.ValidateIdTokenAsync("   "));
        }
    }
}
