namespace Lighthouse.Backend.Models
{
    public class WorkItem : WorkItemBase
    {
        public WorkItem() : base()
        {            
        }

        public WorkItem(WorkItemBase workItemBase, Team team, string parentId) : base(workItemBase)
        {
            Team = team;
            TeamId = team.Id;
            ParentReferenceId = parentId;
        }

        public string ParentReferenceId { get; set; } = string.Empty;

        public Team Team { get; set; }

        public int TeamId { get; set; }
    }
}
