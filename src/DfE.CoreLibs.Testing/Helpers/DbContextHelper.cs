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
                // Sqlite doesn't seem to allow multiple dbContexts added to the same connection
                // We are creating a separate in-memory database for each dbContext
                // Please feel free to update if you have a better/ more efficient solution
                var connection = new SqliteConnection("DataSource=:memory:");

                connection.Open();

                services.AddSingleton(connection);

                services.AddDbContext<TContext>((sp, options) =>
                {
                    options.UseSqlite(connection);
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

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
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
