namespace Lighthouse.Backend.Models
{
    public class WorkTrackingSystemConnectionOption
    {
        public WorkTrackingSystemConnectionOption()
        {            
        }

        public int Id { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public bool IsSecret { get; set; }

        public bool IsOptional { get; set; }

        public int WorkTrackingSystemConnectionId { get; set; }

        public WorkTrackingSystemConnection WorkTrackingSystemConnection { get; set; }
    }
}
