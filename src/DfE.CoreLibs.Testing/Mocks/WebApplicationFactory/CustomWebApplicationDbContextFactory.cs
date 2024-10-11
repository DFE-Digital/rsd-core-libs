using DfE.CoreLibs.Testing.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace DfE.CoreLibs.Testing.Mocks.WebApplicationFactory
{
    [ExcludeFromCodeCoverage]
    public class CustomWebApplicationDbContextFactory<TProgram> : WebApplicationFactory<TProgram>
        where TProgram : class
    {
        public List<Claim>? TestClaims { get; set; } = [];
        public Dictionary<Type, Action<DbContext>>? SeedData { get; set; }
        public Action<IServiceCollection>? ExternalServicesConfiguration { get; set; }
        public Action<HttpClient>? ExternalHttpClientConfiguration { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                foreach (var entry in SeedData ?? [])
                {
                    var dbContextType = entry.Key;
                    var seedAction = entry.Value;

                    var dbContextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<>).MakeGenericType(dbContextType));
                    if (dbContextDescriptor != null)
                    {
                        services.Remove(dbContextDescriptor);
                    }

                    var dbConnectionDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbConnection));
                    if (dbConnectionDescriptor != null)
                    {
                        services.Remove(dbConnectionDescriptor);
                    }


                    var createDbContextMethod = typeof(DbContextHelper).GetMethod(nameof(DbContextHelper.CreateDbContext))
                        ?.MakeGenericMethod(dbContextType);

                    createDbContextMethod?.Invoke(null, new object[] { services, seedAction });
                }

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

        public TDbContext GetDbContext<TDbContext>() where TDbContext : DbContext
        {
            var scopeFactory = Services.GetRequiredService<IServiceScopeFactory>();
            var scope = scopeFactory.CreateScope();
            return scope.ServiceProvider.GetRequiredService<TDbContext>();
        }

    }
}
