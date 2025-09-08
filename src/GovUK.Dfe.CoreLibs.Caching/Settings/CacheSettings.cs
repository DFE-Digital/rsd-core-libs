namespace GovUK.Dfe.CoreLibs.Caching.Settings
{
    public class CacheSettings
    {
        public MemoryCacheSettings Memory { get; set; } = new();
    }

    public class MemoryCacheSettings
    {
        public int DefaultDurationInSeconds { get; set; } = 5;
        public Dictionary<string, int> Durations { get; set; } = new();
    }
}
