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

    public static FileStorageOptions CreateValidLocalFileStorageOptions(string baseDirectory = null, string[] allowedExtensions = null)
    {
        return new FileStorageOptions
        {
            Provider = "Local",
            Local = new LocalFileStorageOptions
            {
                BaseDirectory = baseDirectory ?? Path.Combine(Path.GetTempPath(), "TestFileStorage"),
                CreateDirectoryIfNotExists = true,
                AllowOverwrite = true,
                MaxFileSizeBytes = 100 * 1024 * 1024, // 100MB
                AllowedExtensions = allowedExtensions ?? Array.Empty<string>()
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

    public static Dictionary<string, string> CreateValidLocalConfigurationSettings(string baseDirectory = null, string[] allowedExtensions = null)
    {
        var settings = new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Local",
            ["FileStorage:Local:BaseDirectory"] = baseDirectory ?? Path.Combine(Path.GetTempPath(), "TestFileStorage"),
            ["FileStorage:Local:CreateDirectoryIfNotExists"] = "true",
            ["FileStorage:Local:AllowOverwrite"] = "true",
            ["FileStorage:Local:MaxFileSizeBytes"] = "104857600" // 100MB
        };

        // Add allowed extensions if provided
        if (allowedExtensions != null && allowedExtensions.Length > 0)
        {
            for (int i = 0; i < allowedExtensions.Length; i++)
            {
                settings[$"FileStorage:Local:AllowedExtensions:{i}"] = allowedExtensions[i];
            }
        }

        return settings;
    }
} 