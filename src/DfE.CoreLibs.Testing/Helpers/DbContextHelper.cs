using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace DfE.CoreLibs.Testing.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class DbContextHelper
    {
        public static void CreateDbContext<TContext>(
            IServiceCollection services,
            DbConnection connection,
            Action<TContext>? seedTestData = null) where TContext : DbContext
        {
            ConfigureDbContext<TContext>(services, connection);
            InitializeDbContext(services, seedTestData);
        }

        public static void ConfigureDbContext<TContext>(
            IServiceCollection services,
            DbConnection connection) where TContext : DbContext
        {
            services.AddDbContext<TContext>((sp, options) =>
            {
                options.UseSqlite(connection);
            });
        }

        private static void InitializeDbContext<TContext>(
            IServiceCollection services,
            Action<TContext>? seedTestData) where TContext : DbContext
        {
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

            var relationalDatabaseCreator = dbContext.Database.GetService<IRelationalDatabaseCreator>();
            if (!dbContext.Database.CanConnect())
            {
                relationalDatabaseCreator.Create();
            }
            else
            {
                relationalDatabaseCreator.CreateTables();
            }

            seedTestData?.Invoke(dbContext);
        }
    }
}