using GovUK.Dfe.CoreLibs.ApplicationSettings.Entities;

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Tests.TestHelpers;

public static class TestData
{
    public static ApplicationSetting CreateSetting(
        string key = "TestKey",
        string value = "TestValue",
        string category = "General",
        string? description = null,
        bool isActive = true)
    {
        return new ApplicationSetting
        {
            Key = key,
            Value = value,
            Category = category,
            Description = description,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static List<ApplicationSetting> CreateMultipleSettings()
    {
        return new List<ApplicationSetting>
        {
            CreateSetting("Setting1", "Value1", "General"),
            CreateSetting("Setting2", "Value2", "Security"),
            CreateSetting("Setting3", "Value3", "General"),
            CreateSetting("InactiveSetting", "InactiveValue", "General", isActive: false)
        };
    }
}