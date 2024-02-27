using CMFTAspNet.Models;
using CMFTAspNet.Services.Factories;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Services.Implementation
{
    public class ThroughputService : IThroughputService
    {
        private readonly IWorkItemServiceFactory workItemServiceFactory;

        public ThroughputService(IWorkItemServiceFactory workItemServiceFactory)
        {
            this.workItemServiceFactory = workItemServiceFactory;
        }

        public async Task UpdateThroughput(Team team)
        {
            if (team.WorkTrackingSystem == WorkTrackingSystems.Unknown)
            {
                throw new NotSupportedException("Cannot Update Throughput if Work Tracking System is not set!");
            }

            var workItemService = workItemServiceFactory.GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystem);
            var throughput = await workItemService.GetClosedWorkItemsForTeam(team.ThroughputHistory, team);

            team.UpdateThroughput(throughput);
        }
    }
}
