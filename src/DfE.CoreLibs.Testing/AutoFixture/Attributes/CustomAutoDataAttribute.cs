using AutoFixture.Xunit2;
using DfE.CoreLibs.Testing.AutoFixture.Customizations;
using DfE.CoreLibs.Testing.Helpers;

namespace DfE.CoreLibs.Testing.AutoFixture.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CustomAutoDataAttribute(params Type[] customizations)
        : AutoDataAttribute(() => FixtureFactoryHelper.ConfigureFixtureFactory(CombineCustomizations(customizations)))
    {
        private static Type[] CombineCustomizations(Type[] customizations)
        {
            var defaultCustomizations = new[] { typeof(NSubstituteCustomization) };
            return defaultCustomizations.Concat(customizations).ToArray();
        }
    }
}
