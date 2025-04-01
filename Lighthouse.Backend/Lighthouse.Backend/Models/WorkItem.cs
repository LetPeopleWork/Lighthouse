namespace Lighthouse.Backend.Models
{
    public class WorkItem : WorkItemBase
    {
        public string ParentReferenceId { get; set; } = string.Empty;

        public Team Team { get; set; }

        public int TeamId { get; set; }
    }
}
