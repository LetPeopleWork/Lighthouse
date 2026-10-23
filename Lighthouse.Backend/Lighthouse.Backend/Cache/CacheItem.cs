namespace Lighthouse.Backend.Cache
{
    public class CacheItem<T>
    {
        public CacheItem(T value, TimeSpan expiresAfter, DateTimeOffset created)
        {
            Value = value;
            ExpiresAfter = expiresAfter;
            Created = created;
        }

        public T Value { get; }

        internal DateTimeOffset Created { get; }

        internal TimeSpan ExpiresAfter { get; }
    }
}
