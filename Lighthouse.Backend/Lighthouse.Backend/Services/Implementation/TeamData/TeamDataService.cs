using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.TeamData;

namespace Lighthouse.Backend.Services.Implementation.TeamData
{ 
    public class TeamDataService : ITeamDataService
    {
        private readonly ILogger<TeamDataService> logger;
        private readonly IWorkItemServiceFactory workItemServiceFactory;
        private readonly IWorkItemRepository workItemRepository;
        private readonly ITeamMetricsService teamMetricsService;

        public TeamDataService(ILogger<TeamDataService> logger, IWorkItemServiceFactory workItemServiceFactory, IWorkItemRepository workItemRepository, ITeamMetricsService teamMetricsService)
        {
            this.logger = logger;
            this.workItemServiceFactory = workItemServiceFactory;
            this.workItemRepository = workItemRepository;
            this.teamMetricsService = teamMetricsService;
        }

        public async Task UpdateTeamData(Team team)
        {
            await UpdateWorkItemsForTeam(team);

            teamMetricsService.InvalidateTeamMetrics(team);
            team.RefreshUpdateTime();
            UpdateFeatureWIPForTeam(team, teamMetricsService);

            await workItemRepository.Save();
        }

        private async Task UpdateWorkItemsForTeam(Team team)
        {
            logger.LogInformation("Updating Work Items for Team {TeamName}", team.Name);

            var workItemService = workItemServiceFactory.GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystemConnection.WorkTrackingSystem);
            var items = await workItemService.GetChangedWorkItemsSinceLastTeamUpdate(team);

            foreach (var item in items)
            {
                var existingItem = workItemRepository.GetByPredicate(i => i.ReferenceId == item.ReferenceId);
                if (existingItem == null)
                {
                    workItemRepository.Add(item);
                    logger.LogDebug("Added Work Item {WorkItemId} to DB", item.ReferenceId);
                }
                else
                {
                    existingItem.Update(item);
                    workItemRepository.Update(existingItem);
                    logger.LogDebug("Updated Work Item {WorkItemId} in DB", item.ReferenceId);
                }
            }

            await workItemRepository.Save();
        }

        private static void UpdateFeatureWIPForTeam(Team team, ITeamMetricsService teamMetricsService)
        {
            if (team.AutomaticallyAdjustFeatureWIP)
            {
                var featureWip = teamMetricsService.GetCurrentFeaturesInProgressForTeam(team).Count();

                if (featureWip > 0)
                {
                    team.FeatureWIP = featureWip;
                }
            }
        }
    }
}
