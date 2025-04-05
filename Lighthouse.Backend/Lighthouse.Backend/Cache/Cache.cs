﻿namespace Lighthouse.Backend.Cache
{
    public class Cache<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, CacheItem<TValue>> cache = new Dictionary<TKey, CacheItem<TValue>>();

        public IEnumerable<TKey> Keys { get => cache.Keys; }

        public void Store(TKey key, TValue value, TimeSpan expiresAfter)
        {
            cache[key] = new CacheItem<TValue>(value, expiresAfter);
        }

        public void Remove(TKey key)
        {
            if (cache.ContainsKey(key))
            {
                cache.Remove(key);
            }
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
