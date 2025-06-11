using DfE.CoreLibs.Security.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DfE.CoreLibs.Security.Tests.AuthenticationTests
{
    public class AuthenticationBuilderExtensionsTests
    {
        [Fact]
        public void AddJwtBearerScheme_ReturnsSameBuilder()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services.AddAuthentication();

            // Act
            var returned = builder
                .AddJwtBearerScheme("TestScheme", opts => { });

            // Assert
            Assert.Same(builder, returned);
        }

        [Fact]
        public void AddJwtBearerScheme_ConfiguresOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services.AddAuthentication();

            // Act
            builder.AddJwtBearerScheme("TestScheme", opts =>
            {
                opts.Authority = "https://example.org/";
                opts.Audience = "my-audience";
            });

            var sp = services.BuildServiceProvider();
            var monitor = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var opts = monitor.Get("TestScheme");

            // Assert
            Assert.Equal("https://example.org/", opts.Authority);
            Assert.Equal("my-audience", opts.Audience);
        }

        [Fact]
        public async Task AddJwtBearerScheme_SetsOnMessageReceived_WhenDelegateProvided()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services.AddAuthentication();

            var capture = Substitute.For<ITestCapture>();

            Task OnMsg(MessageReceivedContext ctx)
            {
                capture.Called(ctx);
                return Task.CompletedTask;
            }

            // Act
            builder.AddJwtBearerScheme("TestScheme", _ => { }, OnMsg);

            var sp = services.BuildServiceProvider();
            var monitor = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var opts = monitor.Get("TestScheme");

            Assert.NotNull(opts.Events);
            Assert.NotNull(opts.Events.OnMessageReceived);

            var httpCtx = Substitute.For<HttpContext>();
            var scheme = new AuthenticationScheme("TestScheme", null, typeof(JwtBearerHandler));
            var ctx = new MessageReceivedContext(httpCtx, scheme, opts);

            await opts.Events.OnMessageReceived(ctx);

            capture.Received(1).Called(ctx);
        }

        [Fact]
        public void AddJwtBearerScheme_DoesNotSetOnMessageReceived_WhenDelegateNull()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services.AddAuthentication();

            // Act
            builder.AddJwtBearerScheme("TestScheme", opts => { }, onMessageReceived: null);

            // Build and inspect
            var sp = services.BuildServiceProvider();
            var monitor = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var opts = monitor.Get("TestScheme");

            Assert.NotNull(opts.Events);
            Assert.NotNull(opts.Events.OnMessageReceived);
        }

        public interface ITestCapture
        {
            void Called(MessageReceivedContext ctx);
        }
    }
}
