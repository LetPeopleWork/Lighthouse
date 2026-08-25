using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;
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
        IDomainEventDispatcher domainEventDispatcher,
        IForecastUpdater forecastUpdater)
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

            // Stryker disable once all: what the tests pin about a skipped portfolio is that nothing naming it reaches an operator-visible log level. That is a claim about the level, not about this wording, so a mutant that rewrites the message kills nothing.
            Logger.LogDebug("Last Refresh of Work Items for Project {ProjectName} was {MinutesSinceLastUpdate} Minutes ago - Update should happen after {RefreshAfter} Minutes", entity.Name, minutesSinceLastUpdate, refreshSettings.RefreshAfter);

            return minutesSinceLastUpdate >= refreshSettings.RefreshAfter;
        }

        /// <summary>
        /// Somebody retiring or editing a Delivery while this refresh is already running wins. The
        /// refresh is holding copies read before that happened, so it is not reported as a failure
        /// when its write is refused: the next refresh reads the Deliveries again, and a Delivery
        /// that has been retired is by then not among the ones it is handed at all.
        /// </summary>
        private async Task SaveRecomputedDeliveries(IDeliveryRepository deliveryRepository, Portfolio project)
        {
            if (!await deliveryRepository.TrySaveRecomputedDeliveries())
            {
                Logger.LogInformation(
                    "A Delivery of Portfolio {PortfolioName} was changed while its refresh was running; the refresh leaves it as it now stands and will pick it up next time",
                    project.Name);
            }
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
                    var deliveryRepository = serviceProvider.GetRequiredService<IDeliveryRepository>();
                    var deliveryRuleService = serviceProvider.GetRequiredService<IDeliveryRuleService>();

                    outcome = await workItemService.UpdateFeaturesForPortfolio(project);
                    await domainEventDispatcher.PublishAsync(new PortfolioFeaturesRefreshed(project.Id));

                    var deliverySourceSyncService = serviceProvider.GetRequiredService<IDeliverySourceSyncService>();

                    var deliveries = deliveryRepository.GetRecordableByPortfolio(project.Id);
                    deliveryRuleService.RecomputeRuleBasedDeliveries(project, deliveries);

                    // Both passes decide what a Delivery holds, and both run before the one save, so a
                    // Delivery is written once per refresh however it was chosen. The source pass reads
                    // the Features the fetch above has just brought in, so it cannot move above it.
                    await deliverySourceSyncService.ResyncSourceBoundDeliveries(project, deliveries);

                    await SaveRecomputedDeliveries(deliveryRepository, project);

                    // What this pass resolves and what the forecast below resolves belong to the same
                    // refresh round, and a round reaches the work tracking system once - at the end of
                    // whichever of its executions finishes last.
                    var writeBackTriggerService = serviceProvider.GetRequiredService<IWriteBackTriggerService>();
                    var writeBackCollector = serviceProvider.GetRequiredService<IWriteBackCollector>();

                    writeBackCollector.Stage(
                        project.WorkTrackingSystemConnection,
                        writeBackTriggerService.ResolveFeatureWriteBackForPortfolio(project));

                    // Forecasting here as well would run the unseeded simulation a second time for the
                    // same portfolio in one bulk refresh, and the operator would watch the delivery date
                    // settle and then move. Handing the intent to the forecast updater lets the queue
                    // collapse every request raised during this refresh into a single run.
                    forecastUpdater.TriggerUpdate(project.Id);

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

                    ReportUpdateSummary(serviceProvider, project.Name, outcome, stopwatch.ElapsedMilliseconds, success);
                }
            }
            finally
            {
                await CleanUpOrphanedFeatures();
            }
        }

        private async Task CleanUpOrphanedFeatures()
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
