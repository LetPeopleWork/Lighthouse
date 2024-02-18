namespace CMFTAspNet.WorkTracking
{
    public class WorkTrackingSystemOption
    {
        public WorkTrackingSystemOption()
        {            
        }

        public WorkTrackingSystemOption(string key, string value)
        {
            Key = key;
            Value = value;
        }   

        public string Key { get; set; }

        public string Value { get; set; }
    }
}
