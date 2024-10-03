using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace DfE.CoreLibs.Testing.Mocks.WebApplicationFactory
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
        where TProgram : class
    {
        public List<Claim>? TestClaims { get; set; } = [];
        public Action<IServiceCollection>? ExternalServicesConfiguration { get; set; }
        public Action<HttpClient>? ExternalHttpClientConfiguration { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                ExternalServicesConfiguration?.Invoke(services);

                services.AddSingleton<IEnumerable<Claim>>(sp => TestClaims ?? []);
            });

            builder.UseEnvironment("Development");
        }

        protected override void ConfigureClient(HttpClient client)
        {
            ExternalHttpClientConfiguration?.Invoke(client);

            base.ConfigureClient(client);
        }
    }
}
