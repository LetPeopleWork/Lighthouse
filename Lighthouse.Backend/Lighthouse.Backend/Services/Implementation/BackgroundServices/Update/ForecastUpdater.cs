using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using System.Diagnostics;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class ForecastUpdater(
        ILogger<ForecastUpdater> logger,
        IServiceScopeFactory serviceScopeFactory,
        IUpdateQueueService updateQueueService,
        IDomainEventDispatcher domainEventDispatcher,
        IUpdateStatusStore updateStatusStore)
        : UpdateServiceBase<Portfolio>(logger, serviceScopeFactory, updateQueueService, UpdateType.Forecasts),
            IForecastUpdater
    {
        private readonly IServiceScopeFactory scopeFactory = serviceScopeFactory;
        private readonly IUpdateQueueService queueService = updateQueueService;

        /// <summary>
        /// During a bulk refresh every team that finishes asks for a forecast, and the first one to finish
        /// would produce a date that the teams finishing after it immediately invalidate - the operator sees
        /// a delivery date settle and then move. Waiting until no team of this portfolio is still queued
        /// leaves the last team to finish as the one that forecasts. A team that is already running is not
        /// waited for: a team announces its refresh while its own work is still marked running, so counting
        /// that would make every request wait on the very refresh that asked for it and nothing would ever
        /// be forecast. The wait is a hand-over rather than a skip: the forecast a team asked for is the one
        /// its write is owed, so dropping it would lose that write until the next periodic refresh, and the
        /// last team of a bulk refresh may well be one that failed and therefore never asks at all.
        /// </summary>
        public override void TriggerUpdate(int id)
        {
            var teamsStillWaiting = TeamsOfThePortfolioWaitingToRefresh(id);

            if (teamsStillWaiting.Count > 0)
            {
                queueService.HoldUntilQueuedWorkClears(new UpdateKey(UpdateType.Forecasts, id), teamsStillWaiting, () => QueueForecast(id));
                return;
            }

            QueueForecast(id);
        }

        private void QueueForecast(int id)
        {
            base.TriggerUpdate(id);
        }

        private List<UpdateKey> TeamsOfThePortfolioWaitingToRefresh(int id)
        {
            using var scope = scopeFactory.CreateScope();
            var portfolio = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(id);

            if (portfolio == null)
            {
                return [];
            }

            var teamKeys = portfolio.Teams
                .Select(team => new UpdateKey(UpdateType.Team, team.Id))
                .ToList();

            return updateStatusStore.HasQueuedWork(teamKeys) ? teamKeys : [];
        }

        protected override RefreshSettings GetRefreshSettings()
        {
            throw new NotSupportedException("Forecast Update Service does not support periodic refresh");
        }

        protected override bool ShouldUpdateEntity(Portfolio entity, RefreshSettings refreshSettings)
        {
            throw new NotSupportedException("Forecast Update Service does not support periodic refresh");
        }

        protected override async Task Update(int id, IServiceProvider serviceProvider)
        {
            var portfolioRepo = serviceProvider.GetRequiredService<IRepository<Portfolio>>();

            var portfolio = portfolioRepo.GetById(id);
            if (portfolio == null)
            {
                return;
            }

            var refreshLogService = serviceProvider.GetRequiredService<IRefreshLogService>();
            var stopwatch = Stopwatch.StartNew();
            var success = false;
            var itemCount = 0;

            try
            {
                var forecastService = serviceProvider.GetRequiredService<IForecastService>();
                await forecastService.UpdateForecastsForPortfolio(portfolio);

                var writeBackTriggerService = serviceProvider.GetRequiredService<IWriteBackTriggerService>();
                serviceProvider.GetRequiredService<IWriteBackCollector>().Stage(
                    portfolio.WorkTrackingSystemConnection,
                    writeBackTriggerService.ResolveForecastWriteBackForPortfolio(portfolio));

                await domainEventDispatcher.PublishAsync(new PortfolioForecastsUpdated(portfolio.Id));

                itemCount = portfolio.Features.Count;
                success = true;
            }
            finally
            {
                stopwatch.Stop();

                // Forecasting reads what an earlier refresh already fetched, so it never contacts the
                // work tracking system - there is nothing scanned or downloaded to report here.
                var outcome = SyncOutcome.None;

                await refreshLogService.LogRefreshAsync(new RefreshLog
                {
                    Type = RefreshType.Forecast,
                    EntityId = portfolio.Id,
                    EntityName = portfolio.Name,
                    ItemCount = itemCount,
                    Mode = outcome.Mode,
                    RecordsScanned = outcome.RecordsScanned,
                    RecordsFetched = outcome.RecordsFetched,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ExecutedAt = DateTime.UtcNow,
                    Success = success
                });

                LogUpdateSummary(portfolio.Name, outcome, stopwatch.ElapsedMilliseconds, success);
            }
        }
    }
}
