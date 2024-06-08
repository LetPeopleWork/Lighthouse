using Lighthouse.Models;
using Lighthouse.Services.Factories;
using Lighthouse.Services.Interfaces;
using Lighthouse.WorkTracking;

namespace Lighthouse.Services.Implementation
{
    public class ThroughputService : IThroughputService
    {
        private readonly IWorkItemServiceFactory workItemServiceFactory;
        private readonly ILogger<ThroughputService> logger;

        public ThroughputService(IWorkItemServiceFactory workItemServiceFactory, ILogger<ThroughputService> logger)
        {
            this.workItemServiceFactory = workItemServiceFactory;
            this.logger = logger;
        }

        public async Task UpdateThroughput(Team team)
        {
            logger.LogInformation($"Updating Throughput for Team {team.Name}");

            if (team.WorkTrackingSystem == WorkTrackingSystems.Unknown)
            {
                throw new NotSupportedException("Cannot Update Throughput if Work Tracking System is not set!");
            }

            var workItemService = workItemServiceFactory.GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystem);
            var throughput = await workItemService.GetClosedWorkItems(team.ThroughputHistory, team);

            team.UpdateThroughput(throughput);
            logger.LogInformation($"Finished updating Throughput for Team {team.Name}");
        }
    }
}
