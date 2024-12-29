using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.Update
{
    public class TeamUpdateService : ITeamUpdateService
    {
        private readonly ILogger<TeamUpdateService> logger;
        private readonly IUpdateQueueService updateQueueService;

        public TeamUpdateService(ILogger<TeamUpdateService> logger, IUpdateQueueService updateQueueService)
        {
            this.logger = logger;
            this.updateQueueService = updateQueueService;
        }

        public void TriggerUpdate(int teamId)
        {
            updateQueueService.EnqueueUpdate(UpdateType.Team, teamId, async serviceProvider =>
            {
                var teamRepository = serviceProvider.GetRequiredService<IRepository<Team>>();
                var team = teamRepository.GetById(teamId);
                
                if (team == null)
                {
                    return;
                }

                var workItemServiceFactory = serviceProvider.GetRequiredService<IWorkItemServiceFactory>();
                var workItemService = workItemServiceFactory.GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystemConnection.WorkTrackingSystem);

                await UpdateThroughputForTeam(team, workItemService);
                await UpdateFeatureWipForTeam(team, workItemService);

                await teamRepository.Save();
            });
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
