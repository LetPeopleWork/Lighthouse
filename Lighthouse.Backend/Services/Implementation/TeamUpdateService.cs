using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class TeamUpdateService : ITeamUpdateService
    {
        private readonly IWorkItemServiceFactory workItemServiceFactory;
        private readonly ILogger<TeamUpdateService> logger;

        public TeamUpdateService(IWorkItemServiceFactory workItemServiceFactory, ILogger<TeamUpdateService> logger)
        {
            this.workItemServiceFactory = workItemServiceFactory;
            this.logger = logger;
        }

        public async Task UpdateTeam(Team team)
        {
            await UpdateThroughputForTeam(team);
            await UpdateFeatureWipForTeam(team);
        }

        private async Task UpdateFeatureWipForTeam(Team team)
        {
            logger.LogInformation("Updating Feature Wip for Team {TeamName}", team.Name);

            team.ActualFeatureWIP = 0;

            logger.LogInformation("Finished updating Feature Wip for Team {TeamName}", team.Name);

            await Task.CompletedTask;
        }

        private async Task UpdateThroughputForTeam(Team team)
        {
            logger.LogInformation("Updating Throughput for Team {TeamName}", team.Name);

            var workItemService = workItemServiceFactory.GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystemConnection.WorkTrackingSystem);
            var throughput = await workItemService.GetClosedWorkItems(team.ThroughputHistory, team);

            team.UpdateThroughput(throughput);
            logger.LogInformation("Finished updating Throughput for Team {TeamName}", team.Name);
        }
    }
}
