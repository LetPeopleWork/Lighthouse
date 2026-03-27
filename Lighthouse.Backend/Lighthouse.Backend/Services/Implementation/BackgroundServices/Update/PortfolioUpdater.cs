using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using System.Diagnostics;

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
            using var scope = CreateServiceScope();
            return GetServiceFromServiceScope<IAppSettingService>(scope).GetFeatureRefreshSettings();
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

            var refreshLogService = serviceProvider.GetRequiredService<IRefreshLogService>();
            var stopwatch = Stopwatch.StartNew();
            var success = false;
            var itemCount = 0;

            try
            {
                var workItemService = serviceProvider.GetRequiredService<IWorkItemService>();
                var forecastUpdateService = serviceProvider.GetRequiredService<IForecastService>();
                var projectMetricsService = serviceProvider.GetRequiredService<IPortfolioMetricsService>();
                var deliveryRepository = serviceProvider.GetRequiredService<IDeliveryRepository>();
                var deliveryRuleService = serviceProvider.GetRequiredService<IDeliveryRuleService>();

                await workItemService.UpdateFeaturesForPortfolio(project);
                projectMetricsService.InvalidatePortfolioMetrics(project);

                var deliveries = deliveryRepository.GetByPortfolioAsync(project.Id);
                deliveryRuleService.RecomputeRuleBasedDeliveries(project, deliveries);
                await deliveryRepository.Save();

                var writeBackTriggerService = serviceProvider.GetRequiredService<IWriteBackTriggerService>();
                await writeBackTriggerService.TriggerFeatureWriteBackForPortfolio(project);

                await forecastUpdateService.UpdateForecastsForPortfolio(project);

                await writeBackTriggerService.TriggerForecastWriteBackForPortfolio(project);

                itemCount = project.Features.Count;
                success = true;
            }
            finally
            {
                stopwatch.Stop();
                await refreshLogService.LogRefreshAsync(new RefreshLog
                {
                    Type = RefreshType.Portfolio,
                    EntityId = project.Id,
                    EntityName = project.Name,
                    ItemCount = itemCount,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ExecutedAt = DateTime.UtcNow,
                    Success = success
                });
            }
        }
    }
}
