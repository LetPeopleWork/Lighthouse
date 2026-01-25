using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class PortfolioUpdater(
        ILogger<PortfolioUpdater> logger,
        IServiceScopeFactory serviceScopeFactory,
        IUpdateQueueService updateQueueService)
        : UpdateServiceBase<Portfolio>(logger, serviceScopeFactory, updateQueueService, UpdateType.Features),
            IPortfolioUpdater
    {
        protected override RefreshSettings GetRefreshSettings()
        {
            using (var scope = CreateServiceScope())
            {
                return GetServiceFromServiceScope<IAppSettingService>(scope).GetFeatureRefreshSettings();
            }
        }

        protected override bool ShouldUpdateEntity(Portfolio entity, RefreshSettings refreshSettings)
        {
            var minutesSinceLastUpdate = (DateTime.UtcNow - entity.UpdateTime).TotalMinutes;

            Logger.LogInformation("Last Refresh of Work Items for Project {ProjectName} was {MinutesSinceLastUpdate} Minutes ago - Update should happen after {RefreshAfter} Minutes", entity.Name, minutesSinceLastUpdate, refreshSettings.RefreshAfter);

            return minutesSinceLastUpdate >= refreshSettings.RefreshAfter;
        }

        protected override async Task Update(int id, IServiceProvider serviceProvider)
        {
            var projectRepository = serviceProvider.GetRequiredService<IRepository<Portfolio>>();

            var licenseService = serviceProvider.GetRequiredService<ILicenseService>();
            var projectCount = projectRepository.GetAll().Count();

            if (!licenseService.CanUsePremiumFeatures() && projectCount > 1)
            {
                Logger.LogError("Skipped Refreshing project {TeamId} because the no Premium License was found and there are already {TeamCount} projects", id, projectCount);
                return;
            }

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
