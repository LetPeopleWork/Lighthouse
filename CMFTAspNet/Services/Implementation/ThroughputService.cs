using CMFTAspNet.Models;
using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Factories;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Implementation
{
    public class ThroughputService : IThroughputService
    {
        private readonly IWorkItemServiceFactory workItemServiceFactory;

        public ThroughputService(IWorkItemServiceFactory workItemServiceFactory)
        {
            this.workItemServiceFactory = workItemServiceFactory;
        }

        public async Task UpdateThroughput(int historyInDays, Team team)
        {
            var workItemService = workItemServiceFactory.CreateWorkItemServiceForTeam(team.TeamConfiguration);
            var throughput = await workItemService.GetClosedWorkItemsForTeam(historyInDays, team.TeamConfiguration);
            team.UpdateThroughput(new Throughput(throughput));
        }
    }
}
