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
        /// During a bulk refresh every refresh that finishes asks for a forecast, and the first one to
        /// finish would produce a date that the ones finishing after it immediately invalidate - the
        /// operator sees a delivery date settle and then move. Waiting until every refresh that feeds this
        /// portfolio has finished leaves the last of them as the one that forecasts.
        ///
        /// Both callers ask from inside a refresh whose own key is in that set, and that is what makes the
        /// outcome one forecast rather than a race: the first to ask holds, and because its own run has not
        /// ended the hold cannot have cleared by the time anybody else asks, so every later asker finds the
        /// forecast already promised and stands down. A run leaves the store before the sweep that lets
        /// holds go, so a hold naming the asker is not waiting on itself forever.
        ///
        /// The wait is a hand-over rather than a skip: the forecast a refresh asked for is the one its
        /// write is owed, so dropping it would lose that write until the next periodic refresh, and the
        /// last refresh of a bulk run may well be one that failed and therefore never asks at all.
        /// </summary>
        public override void TriggerUpdate(int id)
        {
            if (AForecastForThisPortfolioIsAlreadyOwed(id))
            {
                return;
            }

            var refreshesStillGoing = TheRefreshesThisForecastWaitsFor(id);

            if (refreshesStillGoing.Count > 0)
            {
                HoldTheForecastUntil(id, refreshesStillGoing);
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
        /// The forecast a waiting request owes, now that the refreshes it waited for have finished.
        /// Unlike a fresh request it never stands down: a forecast someone asked for by hand can have
        /// joined the queue in the meantime, and standing down here would leave the refresh round this
        /// request keeps a place in waiting for a run that never comes - so the write that round collected
        /// would never reach the work tracking system. Anything still going that this run would collide
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
            queueService.HoldUntilNamedWorkClears(
                ForecastKeyFor(portfolioId), waitingOn, () => RunTheWaitingForecast(portfolioId));
        }

        private List<UpdateKey> WorkTheWaitingForecastWouldCollideWith(int portfolioId)
        {
            var forecastKey = ForecastKeyFor(portfolioId);
            var stillToClear = TheRefreshesThisForecastWaitsFor(portfolioId);

            // A forecast somebody asked for by hand can be running at this moment, and a second one would
            // re-run the unseeded simulation over the same data and move the date that one is about to show.
            if (updateStatusStore.HasActiveWork([forecastKey]))
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

        /// <summary>
        /// Everything whose result this forecast reads: the teams delivering the portfolio, and the
        /// portfolio's own Features refresh. The Features refresh is what fetches the features being
        /// forecast, so a forecast that runs while it is mid-fetch is forecasting over half a feature set.
        ///
        /// Work that has started counts as well as work still waiting, because any of these can be running
        /// in another lane at the very moment this is asked, and until its run has ended its writes are not
        /// in.
        /// </summary>
        private List<UpdateKey> TheRefreshesThisForecastWaitsFor(int portfolioId)
        {
            using var scope = scopeFactory.CreateScope();
            var portfolio = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId);

            if (portfolio == null)
            {
                return [];
            }

            var feedingThisForecast = portfolio.Teams
                .Select(team => new UpdateKey(UpdateType.Team, team.Id))
                .Append(new UpdateKey(UpdateType.Features, portfolioId))
                .ToList();

            return updateStatusStore.HasActiveWork(feedingThisForecast) ? feedingThisForecast : [];
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
            var cancelled = false;
            var itemCount = 0;

            try
            {
                itemCount = await ForecastPortfolio(portfolio, serviceProvider);
                success = true;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                throw;
            }
            finally
            {
                stopwatch.Stop();

                await LogForecastRefresh(refreshLogService, portfolio, itemCount, stopwatch.ElapsedMilliseconds, success, cancelled);
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
            IRefreshLogService refreshLogService, Portfolio portfolio, int itemCount, long durationMs, bool success, bool cancelled)
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
                Success = success,
                Cancelled = cancelled
            });
        }
    }
}
