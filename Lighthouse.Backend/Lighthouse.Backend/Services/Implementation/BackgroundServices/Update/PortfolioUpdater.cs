using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
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
        IUpdateQueueService updateQueueService,
        IOrphanedFeatureCleanupService cleanupService,
        IDomainEventDispatcher domainEventDispatcher)
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

            // Stryker disable once all: the portfolio half of AC-1.4's skip trace — what is pinned is that nothing about a skipped entity is operator-visible, which is a claim about level, not wording.
            Logger.LogDebug("Last Refresh of Work Items for Project {ProjectName} was {MinutesSinceLastUpdate} Minutes ago - Update should happen after {RefreshAfter} Minutes", entity.Name, minutesSinceLastUpdate, refreshSettings.RefreshAfter);

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
            var outcome = SyncOutcome.None;

            try
            {
                try
                {
                    var workItemService = serviceProvider.GetRequiredService<IWorkItemService>();
                    var forecastUpdateService = serviceProvider.GetRequiredService<IForecastService>();
                    var deliveryRepository = serviceProvider.GetRequiredService<IDeliveryRepository>();
                    var deliveryRuleService = serviceProvider.GetRequiredService<IDeliveryRuleService>();

                    outcome = await workItemService.UpdateFeaturesForPortfolio(project);
                    await domainEventDispatcher.PublishAsync(new PortfolioFeaturesRefreshed(project.Id));

                    var deliveries = deliveryRepository.GetByPortfolioAsync(project.Id);
                    deliveryRuleService.RecomputeRuleBasedDeliveries(project, deliveries);
                    await deliveryRepository.Save();

                    // Both passes stage into the same collector and reach the tracker once, in the flush
                    // UpdateServiceBase runs at the end of this execution (ADR-144).
                    var writeBackTriggerService = serviceProvider.GetRequiredService<IWriteBackTriggerService>();
                    var writeBackCollector = serviceProvider.GetRequiredService<IWriteBackCollector>();

                    writeBackCollector.Stage(
                        project.WorkTrackingSystemConnection,
                        writeBackTriggerService.ResolveFeatureWriteBackForPortfolio(project));

                    await forecastUpdateService.UpdateForecastsForPortfolio(project);

                    writeBackCollector.Stage(
                        project.WorkTrackingSystemConnection,
                        writeBackTriggerService.ResolveForecastWriteBackForPortfolio(project));

                    await domainEventDispatcher.PublishAsync(new PortfolioForecastsUpdated(project.Id));

                    itemCount = project.Features.Count;
                    success = true;
                }
                catch (UnreadableSecretException exception)
                {
                    outcome = outcome with
                    {
                        Reason = BuildUnreadableSecretReason(
                            exception,
                            project.WorkTrackingSystemConnection,
                            serviceProvider.GetRequiredService<ICryptoService>()),
                    };

                    throw;
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
                        Mode = outcome.Mode,
                        RecordsScanned = outcome.RecordsScanned,
                        RecordsFetched = outcome.RecordsFetched,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        ExecutedAt = DateTime.UtcNow,
                        Success = success
                    });

                    LogUpdateSummary(project.Name, outcome, stopwatch.ElapsedMilliseconds, success);
                }
            }
            finally
            {
                try
                {
                    await cleanupService.CleanupAsync();
                }
#pragma warning disable CA1031 // cleanup failures must not propagate to the refresh queue
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    Logger.LogWarning(ex, "Orphaned-feature cleanup after portfolio update failed (non-fatal)");
                }
            }
        }
    }
}
