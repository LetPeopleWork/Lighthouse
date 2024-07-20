using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.WorkTracking;

namespace Lighthouse.Backend.Services.Implementation
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

        public async Task UpdateThroughputForTeam(Team team)
        {
            logger.LogInformation("Updating Throughput for Team {TeamName}", team.Name);

            var workItemService = workItemServiceFactory.GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystemConnection.WorkTrackingSystem);
            var throughput = await workItemService.GetClosedWorkItems(team.ThroughputHistory, team);

            team.UpdateThroughput(throughput);
            logger.LogInformation("Finished updating Throughput for Team {TeamName}", team.Name);
        }
    }
}
