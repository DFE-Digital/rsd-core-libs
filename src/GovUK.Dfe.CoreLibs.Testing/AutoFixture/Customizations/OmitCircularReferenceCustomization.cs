using AutoFixture;
using System.Diagnostics.CodeAnalysis;

namespace GovUK.Dfe.CoreLibs.Testing.AutoFixture.Customizations
{
    [ExcludeFromCodeCoverage]
    public class OmitCircularReferenceCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => fixture.Behaviors.Remove(b));

            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        }
    }
}
