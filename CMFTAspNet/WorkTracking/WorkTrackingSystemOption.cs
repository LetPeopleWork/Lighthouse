namespace CMFTAspNet.WorkTracking
{
    public class WorkTrackingSystemOption
    {
        public WorkTrackingSystemOption()
        {            
        }

        public WorkTrackingSystemOption(string key, string value, bool isSecret = false)
        {
            Key = key;
            Value = value;
            Secret = isSecret;
        }   

        public int Id { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public bool Secret { get; set; }
    }
}
