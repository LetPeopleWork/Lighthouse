namespace Lighthouse.Backend.Models
{
    public class AdditionalFieldDefinition
    {
        public int Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;

        public int WorkTrackingSystemConnectionId { get; set; }

        public WorkTrackingSystemConnection? WorkTrackingSystemConnection { get; set; }
    }
}
