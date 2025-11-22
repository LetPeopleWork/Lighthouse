
using Lighthouse.Backend.Extensions;

namespace Lighthouse.Backend.Models
{
    public class WorkItem : WorkItemBase
    {
        public WorkItem() : base()
        {            
        }

        public WorkItem(WorkItemBase workItemBase, Team team) : base(workItemBase)
        {
            Team = team;
            TeamId = team.Id;
        }

        public Team Team { get; set; }

        public int TeamId { get; set; }

        public override bool IsBlocked => Team != null && (Team.BlockedStates.IsItemInList(State) || Tags.Any(Team.BlockedTags.IsItemInList));

        internal void Update(WorkItem item)
        {
            base.Update(item);
            Team = item.Team;
            TeamId = item.TeamId;
        }
    }
}
