using AutoFixture;
using AutoMapper;

namespace DfE.CoreLibs.Testing.AutoFixture.Customizations
{
    public class AutoMapperCustomization<TProfile> : ICustomization where TProfile : Profile
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize<IMapper>(composer => composer.FromFactory(() =>
            {
                var profiles = typeof(TProfile).Assembly
                    .GetTypes()
                    .Where(t => typeof(Profile).IsAssignableFrom(t) && !t.IsAbstract)
                    .ToList();

                var config = new MapperConfiguration(cfg =>
                {
                    foreach (var profileInstance in profiles.Select(profileType => (Profile)Activator.CreateInstance(profileType)!))
                    {
                        cfg.AddProfile(profileInstance);
                    }
                });

                return config.CreateMapper();
            }));
        }
    }
}
