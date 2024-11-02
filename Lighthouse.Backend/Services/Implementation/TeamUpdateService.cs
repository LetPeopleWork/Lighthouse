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
            var workItemService = workItemServiceFactory.GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystemConnection.WorkTrackingSystem);

            await UpdateThroughputForTeam(team, workItemService);
            await UpdateFeatureWipForTeam(team, workItemService);
        }

        private async Task UpdateFeatureWipForTeam(Team team, IWorkItemService workItemService)
        {
            logger.LogInformation("Updating Feature Wip for Team {TeamName}", team.Name);

            var featuresInProgress = await workItemService.GetFeaturesInProgressForTeam(team);

            var featureReferences = featuresInProgress.Distinct();
            team.SetFeaturesInProgress(featureReferences);

            logger.LogDebug("Following Features are In Progress: {Features}", string.Join(",", featureReferences));
            logger.LogInformation("Finished updating Feature Wip for Team {TeamName} - Found {FeatureWIP} Features in Progress", team.Name, featureReferences.Count());
        }

        private async Task UpdateThroughputForTeam(Team team, IWorkItemService workItemService)
        {
            logger.LogInformation("Updating Throughput for Team {TeamName}", team.Name);

            var throughput = await workItemService.GetClosedWorkItems(team.ThroughputHistory, team);

            team.UpdateThroughput(throughput);
            logger.LogInformation("Finished updating Throughput for Team {TeamName}", team.Name);
        }
    }
}
