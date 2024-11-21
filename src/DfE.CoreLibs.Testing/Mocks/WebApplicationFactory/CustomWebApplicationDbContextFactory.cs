using DfE.CoreLibs.Testing.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
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
        public List<Claim>? TestClaims { get; set; } = new();
        public Dictionary<Type, Action<DbContext>>? SeedData { get; set; }
        public Action<IServiceCollection>? ExternalServicesConfiguration { get; set; }
        public Action<HttpClient>? ExternalHttpClientConfiguration { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");

            builder.ConfigureServices(services =>
            {
                RemoveDbContextAndConnectionServices(services);

                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();
                services.AddSingleton(connection);

                foreach (var entry in SeedData ?? new Dictionary<Type, Action<DbContext>>())
                {
                    var dbContextType = entry.Key;
                    var seedAction = entry.Value;
                    var createDbContextMethod = typeof(DbContextHelper).GetMethod(nameof(DbContextHelper.CreateDbContext))
                        ?.MakeGenericMethod(dbContextType);
                    createDbContextMethod?.Invoke(null, new object[] { services, connection, seedAction });
                }

                ExternalServicesConfiguration?.Invoke(services);
                services.AddSingleton<IEnumerable<Claim>>(sp => TestClaims ?? new());
            });

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

        private static void RemoveDbContextAndConnectionServices(IServiceCollection services)
        {
            var dbContextDescriptors = services
                .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
                .ToList();
            foreach (var dbContextDescriptor in dbContextDescriptors)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbConnectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));
            if (dbConnectionDescriptor != null)
            {
                services.Remove(dbConnectionDescriptor);
            }
        }
    }
}
