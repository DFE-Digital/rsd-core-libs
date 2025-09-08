using GovUK.Dfe.CoreLibs.Testing.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Claims;
using WireMock.Server;
using WireMock.Settings;

namespace GovUK.Dfe.CoreLibs.Testing.Mocks.WebApplicationFactory
{
    [ExcludeFromCodeCoverage]
    public class CustomWebApplicationDbContextFactory<TProgram> : WebApplicationFactory<TProgram>
        where TProgram : class
    {
        public List<Claim>? TestClaims { get; set; } = new();
        public Dictionary<Type, Action<DbContext>>? SeedData { get; set; }
        public Action<IServiceCollection>? ExternalServicesConfiguration { get; set; }
        public Action<HttpClient>? ExternalHttpClientConfiguration { get; set; }

        public bool UseWireMock { get; set; } = false;
        public int WireMockPort { get; set; } = 0;
        public WireMockServer? WireMockServer { get; private set; }
        public HttpClient? WireMockHttpClient { get; private set; }
        public Action<IServiceCollection, IConfiguration, HttpClient>? ExternalWireMockClientRegistration { get; set; }
        public Action<IConfigurationBuilder, WireMockServer>? ExternalWireMockConfigOverride { get; set; }
        public Action<WireMockServer>? ExternalWireMockStubRegistration { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");

            if (UseWireMock && WireMockServer == null)
            {
                var settings = new WireMockServerSettings
                {
                    Port = WireMockPort
                };
                WireMockServer = WireMockServer.Start(settings);

                WireMockHttpClient = new HttpClient
                {
                    BaseAddress = new Uri(WireMockServer.Urls[0])
                };
                WireMockHttpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", "wiremock-token");

                Console.WriteLine($"WireMock started at: {WireMockServer.Urls[0]}");
            }

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

            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                if (WireMockServer != null && ExternalWireMockConfigOverride != null)
                {
                    ExternalWireMockConfigOverride(cfg, WireMockServer);
                }
            });

            builder.ConfigureServices((ctx, services) =>
            {
                if (ExternalWireMockClientRegistration != null &&
                    WireMockHttpClient != null)
                {
                    ExternalWireMockClientRegistration(
                        services,
                        ctx.Configuration,
                        WireMockHttpClient
                    );
                }
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
