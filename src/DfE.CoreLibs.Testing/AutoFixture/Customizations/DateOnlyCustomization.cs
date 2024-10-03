using AutoFixture;
using System.Diagnostics.CodeAnalysis;

namespace DfE.CoreLibs.Testing.AutoFixture.Customizations
{
    [ExcludeFromCodeCoverage]
    public class DateOnlyCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize<DateOnly>(composer =>
                composer.FromFactory(() =>
                    DateOnly.FromDateTime(fixture.Create<DateTime>())));
        }
    }
}
