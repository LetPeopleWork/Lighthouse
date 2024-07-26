using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    public class FeatureUpdateService : UpdateBackgroundServiceBase
    {
        private readonly ILogger<FeatureUpdateService> logger;

        public FeatureUpdateService(IServiceScopeFactory serviceScopeFactory, ILogger<FeatureUpdateService> logger) : base(serviceScopeFactory, logger)
        {
            this.logger = logger;
        }

        protected override RefreshSettings GetRefreshSettings()
        {
            using (var scope = CreateServiceScope())
            {
                return GetServiceFromServiceScope<IAppSettingService>(scope).GetFeaturRefreshSettings();
            }
        }

        protected override async Task UpdateAllItems(CancellationToken stoppingToken)
        {
            logger.LogInformation("Starting Update of Features for Projects");

            using (var scope = CreateServiceScope())
            {
                var projectRepository = GetServiceFromServiceScope<IRepository<Project>>(scope);

                foreach (var project in projectRepository.GetAll().ToList())
                {
                    logger.LogInformation("Checking Work Items for Project {ProjectName}", project.Name);
                    await UpdateWorkItemsForProject(scope, projectRepository, project);
                }
            }

            logger.LogInformation("Done Updating of Work Items for Projects");
        }

        private async Task UpdateWorkItemsForProject(IServiceScope scope, IRepository<Project> projectRepository, Project project)
        {
            var minutesSinceLastUpdate = (DateTime.UtcNow - project.ProjectUpdateTime).TotalMinutes;
            var refreshSettings = GetRefreshSettings();

            logger.LogInformation("Last Refresh of Work Items for Project {ProjectName} was {minutesSinceLastUpdate} Minutes ago - Update should happen after {RefreshAfter} Minutes", project.Name, minutesSinceLastUpdate, refreshSettings.RefreshAfter);

            if (minutesSinceLastUpdate >= refreshSettings.RefreshAfter)
            {
                var workItemCollectorService = GetServiceFromServiceScope<IWorkItemCollectorService>(scope);
                await workItemCollectorService.UpdateFeaturesForProject(project).ConfigureAwait(true);
                await projectRepository.Save();
            }
        }
    }
}
