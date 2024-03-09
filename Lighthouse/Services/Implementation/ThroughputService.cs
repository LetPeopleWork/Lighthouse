using Lighthouse.Models;
using Lighthouse.Services.Factories;
using Lighthouse.Services.Interfaces;
using Lighthouse.WorkTracking;

namespace Lighthouse.Services.Implementation
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
            var throughput = await workItemService.GetClosedWorkItems(team.ThroughputHistory, team);

            team.UpdateThroughput(throughput);
        }
    }
}
