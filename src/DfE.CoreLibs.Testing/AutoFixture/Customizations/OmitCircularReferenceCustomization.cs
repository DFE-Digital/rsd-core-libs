using AutoFixture;

namespace DfE.CoreLibs.Testing.AutoFixture.Customizations
{
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
