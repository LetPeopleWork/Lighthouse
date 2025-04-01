using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.Update
{
    public class TeamUpdateService : UpdateServiceBase<Team>, ITeamUpdateService
    {
        public TeamUpdateService(ILogger<TeamUpdateService> logger, IServiceScopeFactory serviceScopeFactory, IUpdateQueueService updateQueueService) : base(logger, serviceScopeFactory, updateQueueService, UpdateType.Team)
        {
        }

        protected override RefreshSettings GetRefreshSettings()
        {
            using (var scope = CreateServiceScope())
            {
                return GetServiceFromServiceScope<IAppSettingService>(scope).GetThroughputRefreshSettings();
            }
        }

        protected override async Task Update(int id, IServiceProvider serviceProvider)
        {
            var teamRepository = serviceProvider.GetRequiredService<IRepository<Team>>();
            var team = teamRepository.GetById(id);

            if (team == null)
            {
                return;
            }

            var workItemServiceFactory = serviceProvider.GetRequiredService<IWorkItemServiceFactory>();
            var workItemService = workItemServiceFactory.GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystemConnection.WorkTrackingSystem);

            await UpdateThroughputForTeam(team, workItemService);
            await UpdateFeatureWipForTeam(team, workItemService);

            await teamRepository.Save();
        }

        protected override bool ShouldUpdateEntity(Team entity, RefreshSettings refreshSettings)
        {
            var minutesSinceLastUpdate = (DateTime.UtcNow - entity.TeamUpdateTime).TotalMinutes;

            Logger.LogInformation("Last Refresh of team {TeamName} was {minutesSinceLastUpdate} Minutes ago - Update should happen after {RefreshAfter} Minutes", entity.Name, minutesSinceLastUpdate, refreshSettings.RefreshAfter);

            return minutesSinceLastUpdate >= refreshSettings.RefreshAfter;
        }

        private async Task UpdateFeatureWipForTeam(Team team, IWorkItemService workItemService)
        {
            Logger.LogInformation("Updating Feature Wip for Team {TeamName}", team.Name);

            var featuresInProgress = await workItemService.GetFeaturesInProgressForTeam(team);

            var featureReferences = featuresInProgress.Distinct();
            team.SetFeaturesInProgress(featureReferences);

            Logger.LogDebug("Following Features are In Progress: {Features}", string.Join(",", featureReferences));
            Logger.LogInformation("Finished updating Feature Wip for Team {TeamName} - Found {FeatureWIP} Features in Progress", team.Name, featureReferences.Count());
        }

        private async Task UpdateThroughputForTeam(Team team, IWorkItemService workItemService)
        {
            Logger.LogInformation("Updating Throughput for Team {TeamName}", team.Name);

            var throughput = await workItemService.GetThroughputForTeam(team);
            var items = await workItemService.UpdateWorkItemsForTeam(team);

            // Add Items to DB via Repo and Save
            // Move Throughput and Feature WIP out of Team into Metrics Service
            // Use MetricsService to dynamically calculate Throughput and Feature WIP based on stored Work Items
            // Create MetricsController that uses MetricsService to provide this data to the Frontend
            // Clean up Work Item Service (will be a lot simpler)

            team.UpdateThroughput(throughput);
            Logger.LogInformation("Finished updating Throughput for Team {TeamName}", team.Name);
        }
    }
}
