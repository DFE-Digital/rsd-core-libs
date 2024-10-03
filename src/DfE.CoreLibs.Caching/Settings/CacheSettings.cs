namespace DfE.CoreLibs.Caching.Settings
{
    public class CacheSettings
    {
        public int DefaultDurationInSeconds { get; set; } = 5;
        public Dictionary<string, int> Durations { get; set; } = new();
    }
}
