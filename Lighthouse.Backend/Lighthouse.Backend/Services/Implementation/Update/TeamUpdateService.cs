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

            var workItemRepository = serviceProvider.GetRequiredService<IWorkItemRepository>();
            var teamMetricsService = serviceProvider.GetRequiredService<ITeamMetricsService>();

            await UpdateWorkItemsForTeam(team, workItemService, workItemRepository);

            team.RefreshUpdateTime();

            if (team.AutomaticallyAdjustFeatureWIP)
            {
                var featureWip = teamMetricsService.GetCurrentFeaturesInProgressForTeam(team);
                team.FeatureWIP = featureWip.Count();
            }

            await teamRepository.Save();

            teamMetricsService.InvalidateTeamMetrics(team);
        }

        protected override bool ShouldUpdateEntity(Team entity, RefreshSettings refreshSettings)
        {
            var minutesSinceLastUpdate = (DateTime.UtcNow - entity.TeamUpdateTime).TotalMinutes;

            Logger.LogInformation("Last Refresh of team {TeamName} was {MinutesSinceLastUpdate} Minutes ago - Update should happen after {RefreshAfter} Minutes", entity.Name, minutesSinceLastUpdate, refreshSettings.RefreshAfter);

            return minutesSinceLastUpdate >= refreshSettings.RefreshAfter;
        }

        private async Task UpdateWorkItemsForTeam(Team team, IWorkItemService workItemService, IWorkItemRepository workItemRepository)
        {
            Logger.LogInformation("Updating Work Items for Team {TeamName}", team.Name);
            var items = await workItemService.GetChangedWorkItemsSinceLastTeamUpdate(team);

            foreach (var item in items)
            {
                var existingItem = workItemRepository.GetByPredicate(i => i.ReferenceId == item.ReferenceId);
                if (existingItem == null)
                {
                    workItemRepository.Add(item);
                    Logger.LogDebug("Added Work Item {WorkItemId} to DB", item.ReferenceId);
                }
                else
                {
                    existingItem.Update(item);
                    workItemRepository.Update(existingItem);
                    Logger.LogDebug("Updated Work Item {WorkItemId} in DB", item.ReferenceId);
                }
            }

            await workItemRepository.Save();
        }
    }
}
