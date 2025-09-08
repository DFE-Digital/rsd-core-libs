namespace GovUK.Dfe.CoreLibs.Caching.Interfaces
{
    public interface ICacheService<TCacheType> where TCacheType : ICacheType
    {
        Task<T> GetOrAddAsync<T>(string cacheKey, Func<Task<T>> fetchFunction, string methodName);
        void Remove(string cacheKey);
        Type CacheType { get; }
    }
}
