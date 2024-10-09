using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DfE.CoreLibs.Testing.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class DbContextHelper<TContext> where TContext : DbContext
    {
        public static TContext CreateDbContext(IServiceCollection services, Action<TContext>? seedTestData = null)
        {
            var connectionString = GetConnectionStringFromConfig();

            if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("DataSource=:memory:"))
            {
                var connection = new SqliteConnection(connectionString ?? "DataSource=:memory:");
                connection.Open();

                services.AddSingleton<DbConnection>(_ => connection);
                services.AddDbContext<TContext>((sp, options) =>
                {
                    var conn = sp.GetRequiredService<DbConnection>();
                    options.UseSqlite(conn);
                });
            }
            else
            {
                services.AddDbContext<TContext>(options =>
                {
                    options.UseSqlServer(connectionString);
                });
            }

            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<TContext>();

            dbContext.Database.EnsureCreated();

            seedTestData?.Invoke(dbContext);

            return dbContext;
        }

        private static string? GetConnectionStringFromConfig()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            return configuration.GetConnectionString("DefaultConnection");
        }
    }
}
