using System.Diagnostics;
using System.Threading.Channels;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    /// <summary>
    /// Works out the days a chart was missing, away from the request that noticed them. One queue, one
    /// reader, one scope per pass so each pass gets a database context of its own, and a drain that
    /// empties what is waiting at a moment the caller chooses.
    ///
    /// Deliberately not the shared update queue. That queue serves one owner at a time, shows every
    /// admitted item to the user as a cancellable task, and keeps the database maintenance gate shut
    /// for as long as anything is admitted. Filling in history is none of those things: it is
    /// invisible, interruptible, and every day it writes stays written, so it can stand down at any
    /// point and pick up on the next chart load.
    /// </summary>
    public sealed class OverTimeHistoryFiller : BackgroundService, IOverTimeHistoryFiller
    {
        /// <summary>
        /// How many owners may be waiting at once. Past that an ask is dropped rather than queued -
        /// the next chart load asks again, so nothing is lost by refusing one, whereas an unbounded
        /// queue on a read endpoint is a way to run the instance out of memory from the browser.
        /// </summary>
        private const int MostOwnersWaitingAtOnce = 256;

        // Operator alerting groups on the metric family rather than the metric type, so every failure
        // reported from a percentile pass carries the same value here. A per-type value would split
        // one alert into several.
        private const string MetricFamily = "Percentiles";

        /// <summary>
        /// The longest one pass may keep going. This is not a throughput figure and does not move with
        /// the size of the instance: for as long as a pass is running, an operator who clicks Restore is
        /// refused, so this is how long someone may be left pressing a button that does nothing before
        /// the answer changes. Ten seconds is about as long as anyone waits at a control before deciding
        /// it is broken.
        ///
        /// A pass that reaches it hands the rest of its window back instead of finishing it, and loses
        /// nothing by that: every day already written stays written, and the next chart load asks for
        /// whatever is still missing. So an instance with a lot of history fills its charts in over more
        /// visits rather than holding the operator for longer - which is the direction this is meant to
        /// give way in.
        /// </summary>
        private static readonly TimeSpan LongestOnePassMayRun = TimeSpan.FromSeconds(10);

        private readonly Channel<OverTimeFillRequest> waiting = Channel.CreateBounded<OverTimeFillRequest>(
            new BoundedChannelOptions(MostOwnersWaitingAtOnce) { FullMode = BoundedChannelFullMode.DropWrite });

        private readonly HashSet<(int OwnerId, OwnerType OwnerType, MetricType MetricType)> alreadyAsked = [];

        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<OverTimeHistoryFiller> logger;
        private readonly TimeSpan longestThisPassMayRun;

        private int passesRunning;

        public OverTimeHistoryFiller(IServiceScopeFactory scopeFactory, ILogger<OverTimeHistoryFiller> logger)
            : this(scopeFactory, logger, LongestOnePassMayRun)
        {
        }

        /// <summary>
        /// The same filler on a budget of the caller's choosing. Ten seconds of real waiting is not
        /// something a test can afford to spend, and a budget nothing in the suite can reach is one
        /// nobody can show is actually enforced.
        /// </summary>
        internal OverTimeHistoryFiller(
            IServiceScopeFactory scopeFactory, ILogger<OverTimeHistoryFiller> logger, TimeSpan longestThisPassMayRun)
        {
            this.scopeFactory = scopeFactory;
            this.logger = logger;
            this.longestThisPassMayRun = longestThisPassMayRun;
        }

        public bool HasPassInFlight => Volatile.Read(ref passesRunning) > 0;

        public void AskFor(OverTimeFillRequest request)
        {
            lock (alreadyAsked)
            {
                if (!alreadyAsked.Add(request.Key))
                {
                    return;
                }
            }

            if (waiting.Writer.TryWrite(request))
            {
                return;
            }

            Forget(request.Key);
        }

        public async Task DrainAsync(CancellationToken cancellationToken)
        {
            while (waiting.Reader.TryRead(out var request))
            {
                await RunOnePassAsync(request, cancellationToken);
            }
        }

        /// <summary>
        /// Empties the queue on the way out. A pass interrupted by shutdown loses nothing permanently,
        /// but a day written is a day the next reader does not wait for.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            await DrainAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var request in waiting.Reader.ReadAllAsync(stoppingToken))
            {
                await RunOnePassAsync(request, stoppingToken);
            }
        }

        private async Task RunOnePassAsync(OverTimeFillRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref passesRunning);

            try
            {
                using var scope = scopeFactory.CreateScope();
                await FillAsync(scope.ServiceProvider, request, cancellationToken);
            }
            catch (Exception failure)
            {
                // Nothing escapes. An exception that ended the reader loop would stop history filling
                // itself in for as long as the process lives, with nobody told.
                logger.LogError(
                    failure,
                    "Over-time reconstruction pass failed for {OwnerType} {OwnerId} ({MetricFamily})",
                    request.OwnerType,
                    request.OwnerId,
                    MetricFamily);
            }
            finally
            {
                Forget(request.Key);
                Interlocked.Decrement(ref passesRunning);
            }
        }

        private async Task FillAsync(IServiceProvider services, OverTimeFillRequest request, CancellationToken cancellationToken)
        {
            var target = TargetFor(services, request);
            if (target is null)
            {
                return;
            }

            var writer = services.GetRequiredService<IPercentileSnapshotWriter>();
            var maintenance = services.GetRequiredService<DatabaseMaintenanceGate>();
            var memo = services.GetRequiredService<ReconstructionMemo>();

            try
            {
                await WalkAsync(writer, maintenance, memo, request, target, cancellationToken);
            }
            finally
            {
                // The readings above warmed the shared metrics cache under the same (owner, window)
                // keys the widgets read. Leaving them behind would serve the UI values computed for a
                // day that is not the one it is asking about. Once per pass and for the whole owner:
                // the metrics services evict by owner and nothing finer, so the live entries go with
                // the ninety historical ones, and the dashboard pays one recompute for a pass nobody
                // asked it for. That is the accepted price of not adding a per-key eviction for this.
                target.InvalidateReadCache();
            }
        }

        private async Task WalkAsync(
            IPercentileSnapshotWriter writer,
            DatabaseMaintenanceGate maintenance,
            ReconstructionMemo memo,
            OverTimeFillRequest request,
            PassTarget target,
            CancellationToken cancellationToken)
        {
            // Where the owner's history begins is a property of its stored items, so working it out
            // means a query over them. That is why it happens here rather than where the missing days
            // were noticed: a chart load may cost a scan of the rows it already holds and no more.
            var earliestDayTheItemsSupport = target.EarliestDayTheItemsSupport();
            memo.TheWalkReachesBackNoFurtherThan(
                request.OwnerId, request.OwnerType, request.MetricType, earliestDayTheItemsSupport);

            var timeSpentOnThisPass = Stopwatch.StartNew();
            var daysAlreadyTried = 0;

            foreach (var day in request.CandidateDays)
            {
                // A backup, restore or clear replaces the database file, so writing into it while one
                // runs is what this stands down for. Asked once per day rather than once per pass
                // because a pass outlives the moment it started: the operator may press the button
                // halfway through the walk. Giving up mid-walk costs nothing here - the days already
                // written stay written, and the next chart load asks for whatever is still missing.
                if (maintenance.IsMaintenanceOperationActive)
                {
                    logger.LogInformation(
                        "Over-time reconstruction stood down for {OwnerType} {OwnerId} ({MetricFamily}); a database maintenance operation is running",
                        request.OwnerType,
                        request.OwnerId,
                        MetricFamily);

                    break;
                }

                // The other thing that stops a pass short, and it stops it for the operator's sake
                // rather than the database's: a restore is refused for as long as a pass is running, so
                // how long a pass may run is how long someone can be left at a button that does nothing.
                // The rest of the window is given back rather than hurried, and asked for again on the
                // next chart load.
                //
                // Never before the pass has tried a day. A budget short enough to stop a pass at nothing
                // would be a pass that never finishes a window however many times the chart is opened,
                // which is not a slower fill but no fill at all.
                if (daysAlreadyTried > 0 && timeSpentOnThisPass.Elapsed >= longestThisPassMayRun)
                {
                    logger.LogInformation(
                        "Over-time reconstruction gave the rest of the window back for {OwnerType} {OwnerId} ({MetricFamily}) after {DaysDone} days; the next chart load asks for what is left",
                        request.OwnerType,
                        request.OwnerId,
                        MetricFamily,
                        daysAlreadyTried);

                    break;
                }

                if (cancellationToken.IsCancellationRequested ||
                    IsOutsideWhatTheStoredItemsSupport(day, earliestDayTheItemsSupport, target.LastObservedOn))
                {
                    continue;
                }

                await FillOneDayAsync(writer, memo, request, day, target);
                daysAlreadyTried++;
            }
        }

        /// <summary>
        /// Nothing was stored before the owner's first finished item, and past its last observation
        /// its items are frozen at the break. A reading at either end would draw a confident line over
        /// a period the stored data says nothing about - and because items age out past the owner's
        /// cutoff, a walk of a fixed width regardless of the data reaches further into that with every
        /// day that passes.
        /// </summary>
        private static bool IsOutsideWhatTheStoredItemsSupport(
            DateOnly day, DateOnly? earliestDayTheItemsSupport, DateOnly lastObservedOn)
            => earliestDayTheItemsSupport is null || day < earliestDayTheItemsSupport || day > lastObservedOn;

        /// <summary>
        /// One day, written on its own. A day that cannot be written is one day: the pass carries on
        /// to the next, and the day it skipped is simply still missing when the next chart load looks.
        /// Abandoning the walk here would cost the other eighty-nine days over a single bad one.
        /// </summary>
        private async Task FillOneDayAsync(
            IPercentileSnapshotWriter writer, ReconstructionMemo memo, OverTimeFillRequest request, DateOnly day, PassTarget target)
        {
            try
            {
                writer.FillDayIfAbsent(request.OwnerId, request.OwnerType, request.MetricType, day, target.ReadPercentiles);
                await writer.SaveFilledDay();

                // Worked out once is worked out for good: the reading follows from the owner's stored
                // items, and those change only when the owner is refreshed - which is the event that
                // forgets this again. The day the walk declined to write is the case this exists for,
                // because left unremembered it is found missing by every later chart load, each of
                // which starts another pass that declines it again.
                memo.TheWalkHasAlreadyWorkedOut(request.OwnerId, request.OwnerType, request.MetricType, day);
            }
            catch (Exception failure)
            {
                // Deliberately not remembered: a day lost to a fault is worth another try, unlike one
                // the walk declined on the merits.
                logger.LogError(
                    failure,
                    "Over-time reconstruction could not write {Day} for {OwnerType} {OwnerId} ({MetricFamily}); the rest of the pass continues",
                    day,
                    request.OwnerType,
                    request.OwnerId,
                    MetricFamily);
            }
        }

        private static PassTarget? TargetFor(IServiceProvider services, OverTimeFillRequest request)
        {
            return request.OwnerType switch
            {
                OwnerType.Team => TeamTarget(services, request.OwnerId, request.MetricType),
                OwnerType.Portfolio => PortfolioTarget(services, request.OwnerId, request.MetricType),
                _ => null,
            };
        }

        private static PassTarget? TeamTarget(IServiceProvider services, int teamId, MetricType metricType)
        {
            var team = services.GetRequiredService<IRepository<Team>>().GetById(teamId);
            if (team is null)
            {
                return null;
            }

            var metrics = services.GetRequiredService<ITeamMetricsService>();
            var workItems = services.GetRequiredService<IWorkItemRepository>();
            var clock = services.GetRequiredService<ILighthouseClock>();

            Func<DateTime, DateTime, IEnumerable<PercentileValue>> readPercentiles =
                metricType == MetricType.WorkItemAge
                    ? (_, windowEnd) => metrics.GetWorkItemAgePercentilesForTeam(team, windowEnd)
                    : (windowStart, windowEnd) => metrics.GetCycleTimePercentilesForTeam(team, windowStart, windowEnd);

            return new PassTarget(
                DateOnly.FromDateTime(team.UpdateTime),
                readPercentiles,
                () => metrics.InvalidateTeamMetrics(team),
                () => EarliestFinishedDay(
                    clock,
                    workItems.GetAllByPredicate(item => item.TeamId == teamId && item.ClosedDate != null)
                        .Select(item => item.ClosedDate)));
        }

        private static PassTarget? PortfolioTarget(IServiceProvider services, int portfolioId, MetricType metricType)
        {
            var portfolio = services.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId);
            if (portfolio is null)
            {
                return null;
            }

            var metrics = services.GetRequiredService<IPortfolioMetricsService>();
            var deliveries = services.GetRequiredService<IRepository<Feature>>();
            var clock = services.GetRequiredService<ILighthouseClock>();

            Func<DateTime, DateTime, IEnumerable<PercentileValue>> readPercentiles =
                metricType == MetricType.WorkItemAge
                    ? (_, windowEnd) => metrics.GetWorkItemAgePercentilesForPortfolio(portfolio, windowEnd)
                    : (windowStart, windowEnd) => metrics.GetCycleTimePercentilesForPortfolio(portfolio, windowStart, windowEnd);

            return new PassTarget(
                DateOnly.FromDateTime(portfolio.UpdateTime),
                readPercentiles,
                () => metrics.InvalidatePortfolioMetrics(portfolio),
                () => EarliestFinishedDay(
                    clock,
                    deliveries
                        .GetAllByPredicate(delivery =>
                            delivery.ClosedDate != null && delivery.Portfolios.Any(owner => owner.Id == portfolioId))
                        .Select(delivery => delivery.ClosedDate)));
        }

        /// <summary>
        /// The first day the owner's stored items can support a reading, or null when nothing has ever
        /// finished. Asked of the database as one aggregate rather than by loading the items, because
        /// an owner with a long history has a lot of them and only the earliest one is wanted.
        /// </summary>
        private static DateOnly? EarliestFinishedDay(ILighthouseClock clock, IQueryable<DateTime?> finishedInstants)
        {
            var earliest = finishedInstants.Min();

            return earliest is null ? null : clock.ToInstanceDay(earliest.Value);
        }

        private void Forget((int OwnerId, OwnerType OwnerType, MetricType MetricType) key)
        {
            lock (alreadyAsked)
            {
                alreadyAsked.Remove(key);
            }
        }

        private sealed record PassTarget(
            DateOnly LastObservedOn,
            Func<DateTime, DateTime, IEnumerable<PercentileValue>> ReadPercentiles,
            Action InvalidateReadCache,
            Func<DateOnly?> EarliestDayTheItemsSupport);
    }
}
