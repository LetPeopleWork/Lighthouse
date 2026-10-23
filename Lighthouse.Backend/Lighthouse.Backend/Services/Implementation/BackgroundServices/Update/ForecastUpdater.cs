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
        IUpdateStatusStore updateStatusStore,
        UpdateCancellationContext cancellationContext,
        TimeProvider timeProvider)
        : UpdateServiceBase<Portfolio>(logger, serviceScopeFactory, updateQueueService, timeProvider, UpdateType.Forecasts),
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
            var cancelled = false;
            var skipped = false;
            var itemCount = 0;
            ForecastRunOutcome? outcome = null;

            try
            {
                var forecastWatch = Stopwatch.StartNew();
                outcome = await ForecastPortfolio(portfolio, serviceProvider);
                await ConfirmTheForecastsAreCurrent(outcome, serviceProvider);
                await PassTheForecastOn(portfolio, forecastWatch.ElapsedMilliseconds, serviceProvider);
                await RecordWhatTheForecastWasComputedFrom(portfolio.Id, outcome, serviceProvider);

                itemCount = portfolio.Features.Count;
                skipped = WasSkipped(outcome);
                success = true;
            }
            catch (OperationCanceledException) when (cancellationContext.Current.IsCancellationRequested)
            {
                cancelled = true;
                throw;
            }
            finally
            {
                stopwatch.Stop();

                if (!success && outcome is not ForecastRunOutcome.Computed)
                {
                    await ForgetWhatTheForecastWasComputedFrom(portfolio.Id);
                }

                await LogForecastRefresh(refreshLogService, portfolio, itemCount, stopwatch.ElapsedMilliseconds, success, cancelled, skipped);
                ReportForecastSummary(serviceProvider, portfolio.Name, stopwatch.ElapsedMilliseconds, success, cancelled, skipped);
            }
        }

        private Task<ForecastRunOutcome> ForecastPortfolio(Portfolio portfolio, IServiceProvider serviceProvider)
        {
            var instanceDay = serviceProvider.GetRequiredService<ILighthouseClock>().Today;

            return serviceProvider.GetRequiredService<IForecastService>().UpdateForecastsForPortfolio(portfolio, instanceDay, cancellationContext.Current);
        }

        /// <summary>
        /// A skipped run still stages its write-back and announces itself, because what listens for the
        /// announcement (the day's snapshot, the write-back of forecast dates) does not care whether the
        /// numbers were drawn again, only that they are current.
        /// </summary>
        /// <param name="forecastMs">How long the forecast took, from gathering its inputs to saving or confirming what it produced.</param>
        private async Task PassTheForecastOn(Portfolio portfolio, long forecastMs, IServiceProvider serviceProvider)
        {
            var publishWatch = Stopwatch.StartNew();
            await StageTheWriteBackAndAnnounce(portfolio, serviceProvider);
            var publishMs = publishWatch.ElapsedMilliseconds;

            Logger.LogDebug("Forecast round timings | Portfolio '{Portfolio}' | forecast={ForecastMs}ms | publish={PublishMs}ms",
                portfolio.Name, forecastMs, publishMs);
        }

        private static bool WasSkipped(ForecastRunOutcome outcome) => outcome is ForecastRunOutcome.ConfirmedUnchanged;

        private async Task StageTheWriteBackAndAnnounce(Portfolio portfolio, IServiceProvider serviceProvider)
        {
            var writeBackTriggerService = serviceProvider.GetRequiredService<IWriteBackTriggerService>();
            serviceProvider.GetRequiredService<IWriteBackCollector>().Stage(
                portfolio.WorkTrackingSystemConnection,
                writeBackTriggerService.ResolveForecastWriteBackForPortfolio(portfolio));

            await domainEventDispatcher.PublishAsync(new PortfolioForecastsUpdated(portfolio.Id));
        }

        /// <summary>
        /// A skipped forecast was not drawn again, but it was checked against current data, so every Feature
        /// it covers reads as last updated now rather than at the run that computed it. The time is taken the
        /// same way a computed forecast takes its own.
        /// </summary>
        private async Task ConfirmTheForecastsAreCurrent(ForecastRunOutcome outcome, IServiceProvider serviceProvider)
        {
            if (outcome is not ForecastRunOutcome.ConfirmedUnchanged confirmed)
            {
                return;
            }

            await serviceProvider.GetRequiredService<IFeatureRepository>()
                .ConfirmForecastsAsOf(confirmed.FeatureIds, CurrentInstant, cancellationContext.Current);
        }

        /// <summary>
        /// Recorded last, once everything the run owes is staged and announced: a recorded value is the
        /// promise that the stored forecasts are current, and a later quiet round trusts it without looking.
        /// The inputs replace only the mark the run put down before saving, so a run that fails anywhere in
        /// between, or a press or a restart that changed the value meanwhile, leaves the next round to compute.
        /// A run that could not fingerprint its inputs leaves its mark, which computes next time too.
        /// </summary>
        private async Task RecordWhatTheForecastWasComputedFrom(int portfolioId, ForecastRunOutcome outcome, IServiceProvider serviceProvider)
        {
            if (outcome is not ForecastRunOutcome.Computed { FreshFingerprint: { } fresh } computed)
            {
                return;
            }

            var recorded = computed.RunMark is { } runMark
                && await serviceProvider.GetRequiredService<IPortfolioRepository>()
                    .TryRecordForecastInputs(portfolioId, runMark, fresh, cancellationContext.Current);

            if (!recorded)
            {
                Logger.LogDebug("Forecast inputs of Portfolio {Id} not recorded: a press or a restart landed during the run", portfolioId);
            }
        }

        /// <summary>
        /// A run that failed or was stopped before the forecast answered, or part way through confirming an
        /// unchanged one, can leave what was recorded before it standing, and the next round must not trust it.
        /// A run that answered with a computed forecast needs none of this: the mark it put down before saving
        /// already stands in place of the inputs. Forgetting is best effort and must not turn a failed forecast
        /// into a different failure, so it runs in a scope of its own and is never cancelled.
        /// </summary>
        private async Task ForgetWhatTheForecastWasComputedFrom(int portfolioId)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IPortfolioRepository>()
                    .ForgetForecastInputs([portfolioId], CancellationToken.None);
            }
            // The forecast already failed; failing to forget its inputs must not replace that failure.
#pragma warning disable CA1031
            catch (Exception exception)
#pragma warning restore CA1031
            {
                Logger.LogWarning(exception, "Could not forget the forecast inputs of Portfolio {Id}: {Exception}", portfolioId, exception.Message);
            }
        }

        /// <summary>
        /// Forecasting reads what an earlier refresh already fetched, so it never contacts the work
        /// tracking system - there is nothing scanned or downloaded to record.
        /// </summary>
        private Task LogForecastRefresh(
            IRefreshLogService refreshLogService, Portfolio portfolio, int itemCount, long durationMs, bool success, bool cancelled, bool skipped)
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
                ExecutedAt = CurrentInstant,
                Success = success,
                Cancelled = cancelled,
                Skipped = skipped
            });
        }
    }
}
