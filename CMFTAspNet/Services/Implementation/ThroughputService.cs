using CMFTAspNet.Models;
using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Implementation
{
    public class ThroughputService : IThroughputService
    {
        private readonly Team team;
        private readonly IWorkItemService workItemService;

        public ThroughputService(Team team, IWorkItemService workItemService)
        {
            this.team = team;
            this.workItemService = workItemService;
        }

        public async Task UpdateThroughput(int historyInDays)
        {
            var throughput = await workItemService.GetClosedWorkItemsForTeam(historyInDays, team.TeamConfiguration);
            team.UpdateThroughput(new Throughput(throughput));
        }
    }
}
