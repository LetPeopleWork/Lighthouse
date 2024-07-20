using Lighthouse.Backend.WorkTracking;

namespace Lighthouse.Backend.Models
{
    public class WorkTrackingSystemOptionsOwner<T> : IWorkItemQueryOwner where T : class
    {
        public int Id { get; set; }

        public string WorkItemQuery { get; set; } = string.Empty;

        public int WorkTrackingSystemConnectionId { get; set; }

        public WorkTrackingSystemConnection WorkTrackingSystemConnection { get; set; }
    }
}
