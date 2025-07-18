using Microsoft.Extensions.Configuration;
using System.Text;
using DfE.CoreLibs.FileStorage.Settings;

namespace DfE.CoreLibs.FileStorage.Tests;

public static class TestHelpers
{
    public static IConfiguration CreateConfiguration(Dictionary<string, string> settings)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(settings);
        return configurationBuilder.Build();
    }

    public static Stream CreateTestStream(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }

    public static string ReadStreamContent(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static FileStorageOptions CreateValidFileStorageOptions()
    {
        return new FileStorageOptions
        {
            Provider = "Azure",
            Azure = new AzureFileStorageOptions
            {
                ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net",
                ShareName = "testshare"
            }
        };
    }

    public static Dictionary<string, string> CreateValidConfigurationSettings()
    {
        return new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Azure",
            ["FileStorage:Azure:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net",
            ["FileStorage:Azure:ShareName"] = "testshare"
        };
    }
} 