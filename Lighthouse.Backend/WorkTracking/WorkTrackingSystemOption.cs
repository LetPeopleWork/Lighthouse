namespace Lighthouse.Backend.WorkTracking
{
    public class WorkTrackingSystemOption<T> where T : class
    {
        public WorkTrackingSystemOption()
        {
        }

        public WorkTrackingSystemOption(string key, string value, bool isSecret)
        {
            Key = key;
            Value = value;
            Secret = isSecret;
        }

        public int Id { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public bool Secret { get; set; }

        public T? Entity { get; set; }

        public int EntityId { get; set; }
    }
}
