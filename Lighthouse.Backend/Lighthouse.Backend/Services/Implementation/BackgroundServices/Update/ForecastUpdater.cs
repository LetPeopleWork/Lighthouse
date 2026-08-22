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
            if (AForecastForThisPortfolioIsAlreadyOwed(id))
            {
                return;
            }

            var teamsStillWaiting = TeamsOfThePortfolioWaitingToRefresh(id);

            if (teamsStillWaiting.Count > 0)
            {
                HoldTheForecastUntil(id, teamsStillWaiting);
                return;
            }

            base.TriggerUpdate(id);
        }

        /// <summary>
        /// Forecasts now, for a person who asked for one and is watching for the answer. The waiting above
        /// is there for a bulk refresh nobody asked for; someone who pressed a button and was told it
        /// worked has to see it happen, so this run goes ahead whatever else is in flight. A forecast that
        /// was already waiting is left exactly where it is: it runs once the teams it waits for have
        /// landed, over data this run could not have seen.
        /// </summary>
        public void TriggerImmediateUpdate(int id)
        {
            base.TriggerUpdate(id);
        }

        /// <summary>
        /// The forecast a waiting request owes, now that the teams it waited for have left the queue.
        /// Unlike a fresh request it never stands down: a forecast someone asked for by hand can have
        /// joined the queue in the meantime, and standing down here would leave the refresh round this
        /// request keeps a place in waiting for a run that never comes - so the write that round collected
        /// would never reach the work tracking system. Anything still queued that this run would collide
        /// with is waited for instead, which passes the place on rather than giving it up.
        /// </summary>
        private void RunTheWaitingForecast(int portfolioId)
        {
            var stillToClear = WorkTheWaitingForecastWouldCollideWith(portfolioId);

            if (stillToClear.Count > 0)
            {
                HoldTheForecastUntil(portfolioId, stillToClear);
                return;
            }

            base.TriggerUpdate(portfolioId);
        }

        private void HoldTheForecastUntil(int portfolioId, IReadOnlyCollection<UpdateKey> waitingOn)
        {
            queueService.HoldUntilQueuedWorkClears(
                ForecastKeyFor(portfolioId), waitingOn, () => RunTheWaitingForecast(portfolioId));
        }

        private List<UpdateKey> WorkTheWaitingForecastWouldCollideWith(int portfolioId)
        {
            var forecastKey = ForecastKeyFor(portfolioId);
            var stillToClear = TeamsOfThePortfolioWaitingToRefresh(portfolioId);

            if (updateStatusStore.HasQueuedWork([forecastKey]))
            {
                stillToClear.Add(forecastKey);
            }

            return stillToClear;
        }

        /// <summary>
        /// A forecast that is parked, or sitting in the queue and not started, has not read anything yet, so
        /// it will see whatever the caller just wrote. Asking for a second one would only run the unseeded
        /// simulation again and move a date the first one is about to show. A forecast that is already
        /// running deliberately does not count: it read its data before this request existed, so that
        /// request still needs a run of its own.
        /// </summary>
        private bool AForecastForThisPortfolioIsAlreadyOwed(int portfolioId)
        {
            var forecastKey = ForecastKeyFor(portfolioId);

            return queueService.IsHeld(forecastKey) || updateStatusStore.HasQueuedWork([forecastKey]);
        }

        private List<UpdateKey> TeamsOfThePortfolioWaitingToRefresh(int portfolioId)
        {
            using var scope = scopeFactory.CreateScope();
            var portfolio = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId);

            if (portfolio == null)
            {
                return [];
            }

            var teamKeys = portfolio.Teams
                .Select(team => new UpdateKey(UpdateType.Team, team.Id))
                .ToList();

            return updateStatusStore.HasQueuedWork(teamKeys) ? teamKeys : [];
        }

        private static UpdateKey ForecastKeyFor(int portfolioId)
        {
            return new UpdateKey(UpdateType.Forecasts, portfolioId);
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
            var portfolio = serviceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(id);

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
                itemCount = await ForecastPortfolio(portfolio, serviceProvider);
                success = true;
            }
            finally
            {
                stopwatch.Stop();

                await LogForecastRefresh(refreshLogService, portfolio, itemCount, stopwatch.ElapsedMilliseconds, success);
                ReportForecastSummary(serviceProvider, portfolio.Name, stopwatch.ElapsedMilliseconds, success);
            }
        }

        private async Task<int> ForecastPortfolio(Portfolio portfolio, IServiceProvider serviceProvider)
        {
            await serviceProvider.GetRequiredService<IForecastService>().UpdateForecastsForPortfolio(portfolio);

            var writeBackTriggerService = serviceProvider.GetRequiredService<IWriteBackTriggerService>();
            serviceProvider.GetRequiredService<IWriteBackCollector>().Stage(
                portfolio.WorkTrackingSystemConnection,
                writeBackTriggerService.ResolveForecastWriteBackForPortfolio(portfolio));

            await domainEventDispatcher.PublishAsync(new PortfolioForecastsUpdated(portfolio.Id));

            return portfolio.Features.Count;
        }

        /// <summary>
        /// Forecasting reads what an earlier refresh already fetched, so it never contacts the work
        /// tracking system - there is nothing scanned or downloaded to record.
        /// </summary>
        private static Task LogForecastRefresh(
            IRefreshLogService refreshLogService, Portfolio portfolio, int itemCount, long durationMs, bool success)
        {
            var nothingFetched = SyncOutcome.None;

            return refreshLogService.LogRefreshAsync(new RefreshLog
            {
                Type = RefreshType.Forecast,
                EntityId = portfolio.Id,
                EntityName = portfolio.Name,
                ItemCount = itemCount,
                Mode = nothingFetched.Mode,
                RecordsScanned = nothingFetched.RecordsScanned,
                RecordsFetched = nothingFetched.RecordsFetched,
                DurationMs = durationMs,
                ExecutedAt = DateTime.UtcNow,
                Success = success
            });
        }
    }
}
