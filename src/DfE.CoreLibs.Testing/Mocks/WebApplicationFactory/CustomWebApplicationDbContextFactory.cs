using DfE.CoreLibs.Testing.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.Security.Claims;

namespace DfE.CoreLibs.Testing.Mocks.WebApplicationFactory
{
    public class CustomWebApplicationDbContextFactory<TProgram, TDbContext> : WebApplicationFactory<TProgram>
        where TProgram : class where TDbContext : DbContext
    {
        public List<Claim>? TestClaims { get; set; } = [];
        public Action<TDbContext>? SeedData { get; set; }
        public Action<IServiceCollection>? ExternalServicesConfiguration { get; set; }
        public Action<HttpClient>? ExternalHttpClientConfiguration { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TDbContext>));
                services.Remove(dbContextDescriptor!);

                var dbConnectionDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbConnection));
                services.Remove(dbConnectionDescriptor!);

                DbContextHelper<TDbContext>.CreateDbContext(services, SeedData);

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

        public TDbContext GetDbContext()
        {
            var scopeFactory = Services.GetRequiredService<IServiceScopeFactory>();
            var scope = scopeFactory.CreateScope();
            return scope.ServiceProvider.GetRequiredService<TDbContext>();
        }

    }
}
