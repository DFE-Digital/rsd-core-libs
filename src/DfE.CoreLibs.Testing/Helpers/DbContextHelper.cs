using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DfE.CoreLibs.Testing.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class DbContextHelper
    {
        public static void CreateDbContext<TContext>(IServiceCollection services, Action<TContext>? seedTestData = null) where TContext : DbContext
        {
            var connectionString = GetConnectionStringFromConfig();

            if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("DataSource=:memory:"))
            {
                var connection = new SqliteConnection(connectionString ?? "DataSource=:memory:");
                connection.Open();

                services.AddSingleton<DbConnection>(_ => connection);
                services.AddDbContext<TContext>((sp, options) =>
                {
                    options.UseSqlite(sp.GetRequiredService<DbConnection>());
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
