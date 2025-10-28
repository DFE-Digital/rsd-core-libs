namespace GovUK.Dfe.CoreLibs.Caching.Settings
{
    public class CacheSettings
    {
        public MemoryCacheSettings Memory { get; set; } = new();
        public RedisCacheSettings Redis { get; set; } = new();
    }

    public class MemoryCacheSettings
    {
        public int DefaultDurationInSeconds { get; set; } = 5;
        public Dictionary<string, int> Durations { get; set; } = new();
    }

    public class RedisCacheSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int DefaultDurationInSeconds { get; set; } = 300; // 5 minutes default for Redis
        public Dictionary<string, int> Durations { get; set; } = new();
        public string KeyPrefix { get; set; } = "DfE:Cache:";
        public int Database { get; set; } = 0;
    }
}
