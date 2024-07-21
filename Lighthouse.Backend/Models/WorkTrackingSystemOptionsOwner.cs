namespace Lighthouse.Backend.Models
{
    public class WorkTrackingSystemOptionsOwner : IWorkItemQueryOwner
    {
        public int Id { get; set; }

        public string WorkItemQuery { get; set; } = string.Empty;

        public int WorkTrackingSystemConnectionId { get; set; }

        public WorkTrackingSystemConnection WorkTrackingSystemConnection { get; set; }
    }
}
