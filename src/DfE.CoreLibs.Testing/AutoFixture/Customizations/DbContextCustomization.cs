using AutoFixture;
using DfE.CoreLibs.Testing.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace DfE.CoreLibs.Testing.AutoFixture.Customizations
{
    [ExcludeFromCodeCoverage]
    public class DbContextCustomization<TContext> : ICustomization where TContext : DbContext
    {
        public void Customize(IFixture fixture)
        {
            fixture.Register<DbSet<object>>(() => null!);

            fixture.Customize<TContext>(composer => composer.FromFactory(() =>
            {
                var services = new ServiceCollection();
                var dbContext = DbContextHelper<TContext>.CreateDbContext(services);
                fixture.Inject(dbContext);
                return dbContext;
            }).OmitAutoProperties());
        }
    }
}
