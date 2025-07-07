using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class ProjectUpdater : UpdateServiceBase<Project>, IProjectUpdater
    {
        public ProjectUpdater(
            ILogger<ProjectUpdater> logger, IServiceScopeFactory serviceScopeFactory, IUpdateQueueService updateQueueService)
            : base(logger, serviceScopeFactory, updateQueueService, UpdateType.Features)
        {
        }

        protected override RefreshSettings GetRefreshSettings()
        {
            using (var scope = CreateServiceScope())
            {
                return GetServiceFromServiceScope<IAppSettingService>(scope).GetFeaturRefreshSettings();
            }
        }

        protected override bool ShouldUpdateEntity(Project entity, RefreshSettings refreshSettings)
        {
            var minutesSinceLastUpdate = (DateTime.UtcNow - entity.UpdateTime).TotalMinutes;

            Logger.LogInformation("Last Refresh of Work Items for Project {ProjectName} was {MinutesSinceLastUpdate} Minutes ago - Update should happen after {RefreshAfter} Minutes", entity.Name, minutesSinceLastUpdate, refreshSettings.RefreshAfter);

            return minutesSinceLastUpdate >= refreshSettings.RefreshAfter;
        }

        protected override async Task Update(int id, IServiceProvider serviceProvider)
        {
            var projectRepository = serviceProvider.GetRequiredService<IRepository<Project>>();
            
            var project = projectRepository.GetById(id);
            if (project == null)
            {
                return;
            }

            var workItemService = serviceProvider.GetRequiredService<IWorkItemService>();
            var forecastUpdateService = serviceProvider.GetRequiredService<IForecastService>();
            var projectMetricsService = serviceProvider.GetRequiredService<IProjectMetricsService>();

            await workItemService.UpdateFeaturesForProject(project);
            projectMetricsService.InvalidateProjectMetrics(project);

            await forecastUpdateService.UpdateForecastsForProject(project);
        }
    }
}
