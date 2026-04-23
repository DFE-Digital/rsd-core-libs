using GovUK.Dfe.CoreLibs.Security.EntraSso;

namespace GovUK.Dfe.CoreLibs.Security.Tests.EntraSsoTests
{
    public class EntraSsoDefaultsTests
    {
        [Fact]
        public void AuthenticationScheme_ShouldBeEntraSso()
        {
            Assert.Equal("EntraSso", EntraSsoDefaults.AuthenticationScheme);
        }

        [Fact]
        public void BearerScheme_ShouldBeEntraSsoBearer()
        {
            Assert.Equal("EntraSsoBearer", EntraSsoDefaults.BearerScheme);
        }

        [Fact]
        public void ConfigurationSection_ShouldBeEntraSso()
        {
            Assert.Equal("EntraSso", EntraSsoDefaults.ConfigurationSection);
        }

        [Fact]
        public void SchemeNames_ShouldBeDifferent()
        {
            Assert.NotEqual(EntraSsoDefaults.AuthenticationScheme, EntraSsoDefaults.BearerScheme);
        }
    }
}
