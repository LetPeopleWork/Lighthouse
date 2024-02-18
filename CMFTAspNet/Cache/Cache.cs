namespace CMFTAspNet.Cache
{
    public class Cache<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, CacheItem<TValue>> cache = new Dictionary<TKey, CacheItem<TValue>>();

        public void Store(TKey key, TValue value, TimeSpan expiresAfter)
        {
            cache[key] = new CacheItem<TValue>(value, expiresAfter);
        }

        public TValue? Get(TKey key)
        {
            if (!cache.ContainsKey(key))
            {
                return default;
            }

            var cached = cache[key];
            if (DateTimeOffset.Now - cached.Created >= cached.ExpiresAfter)
            {
                cache.Remove(key);
                return default;
            }

            return cached.Value;
        }
    }
}
