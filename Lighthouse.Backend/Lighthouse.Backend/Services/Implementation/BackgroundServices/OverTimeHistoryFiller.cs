using System.Diagnostics;
using System.Threading.Channels;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.BackgroundServices;

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

        // Operator alerting groups on the work rather than on the chart, and one pass covers every
        // chart an owner has, so every failure reported from a pass carries the same value here. A
        // per-chart value would split one alert into several.
        private const string MetricFamily = "OverTime";

        /// <summary>
        /// The most days one pass works out. A year-wide picker then fills over successive loads
        /// rather than in one unbounded walk, and every day written stays written, so the next load
        /// carries on from where this one stopped. Only days the pass actually works out count: one
        /// that falls outside what the owner's stored items support is stepped over for nothing, and
        /// counting it would let a period reaching back past the owner's history spend the whole pass
        /// on days that can never be written, leaving out the part the history does cover.
        /// </summary>
        private const int MostDaysOnePassWorksOut = 90;

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

        private readonly HashSet<(int OwnerId, OwnerType OwnerType)> alreadyAsked = [];

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
            try
            {
                using var scope = scopeFactory.CreateScope();

                // Settled before the pass counts as running, so an ask dropped here never shows the
                // maintenance gate a fill that is not happening. Read from this pass's own scope because
                // the filler lives as long as the process and would otherwise see the switch as it stood
                // at start-up. A pass that got past this point finishes even if the switch goes off.
                if (!scope.ServiceProvider.GetRequiredService<IOverTimeHistoryFillSwitch>().IsSwitchedOn())
                {
                    logger.LogInformation(
                        "Over-time reconstruction dropped a waiting pass for {OwnerType} {OwnerId} ({MetricFamily}); filling in past days is switched off",
                        request.OwnerType,
                        request.OwnerId,
                        MetricFamily);

                    return;
                }

                await FillWhileCountedAsRunningAsync(scope.ServiceProvider, request, cancellationToken);
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
            }
        }

        private async Task FillWhileCountedAsRunningAsync(
            IServiceProvider services, OverTimeFillRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref passesRunning);

            try
            {
                await FillAsync(services, request, cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref passesRunning);
            }
        }

        private async Task FillAsync(IServiceProvider services, OverTimeFillRequest request, CancellationToken cancellationToken)
        {
            var target = OverTimeFillTarget.For(services, request);
            if (target is null)
            {
                return;
            }

            var writers = new OverTimeWriters(
                services.GetRequiredService<IPercentileSnapshotWriter>(),
                services.GetRequiredService<IProcessBehaviorSnapshotWriter>());
            var maintenance = services.GetRequiredService<DatabaseMaintenanceGate>();
            var memo = services.GetRequiredService<ReconstructionMemo>();

            try
            {
                await WalkAsync(writers, maintenance, memo, request, target, cancellationToken);
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
            OverTimeWriters writers,
            DatabaseMaintenanceGate maintenance,
            ReconstructionMemo memo,
            OverTimeFillRequest request,
            OverTimeFillTarget target,
            CancellationToken cancellationToken)
        {
            // Where the owner's history begins is a property of its stored items, so working it out
            // means a query over them. That is why it happens here rather than where the missing days
            // were noticed: a chart load may cost a scan of the rows it already holds and no more.
            var earliestDayTheItemsSupport = target.EarliestDayTheItemsSupport();
            memo.TheWalkReachesBackNoFurtherThan(
                request.OwnerId, request.OwnerType, earliestDayTheItemsSupport);

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

                if (daysAlreadyTried >= MostDaysOnePassWorksOut)
                {
                    break;
                }

                if (cancellationToken.IsCancellationRequested ||
                    IsOutsideWhatTheStoredItemsSupport(day, earliestDayTheItemsSupport, target.LastObservedOn))
                {
                    continue;
                }

                await FillOneDayAsync(writers, memo, request, day, target);
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
        /// One day of every chart the owner has, written on its own. A day that cannot be written is
        /// one day: the pass carries on to the next, and the day it skipped is simply still missing
        /// when the next chart load looks. Abandoning the walk here would cost the other eighty-nine
        /// days over a single bad one.
        ///
        /// The two kinds of chart are staged and committed separately so that a fault in one leaves
        /// the other's rows written rather than discarding them - but the day is only settled once
        /// both got through, because a day remembered as settled is never offered to a pass again.
        /// </summary>
        private async Task FillOneDayAsync(
            OverTimeWriters writers, ReconstructionMemo memo, OverTimeFillRequest request, DateOnly day, OverTimeFillTarget target)
        {
            var percentilesAreDone = await TryWriteDayAsync(
                request,
                day,
                () =>
                {
                    foreach (var family in target.PercentileFamilies)
                    {
                        writers.Percentiles.FillDayIfAbsent(request.OwnerId, request.OwnerType, family.MetricType, day, family.ReadPercentiles);
                    }
                },
                writers.Percentiles.SaveFilledDay);

            var limitsAreDone = await TryWriteDayAsync(
                request,
                day,
                () =>
                {
                    foreach (var family in target.ProcessBehaviorFamilies)
                    {
                        writers.ProcessBehavior.FillDayIfAbsent(request.OwnerId, request.OwnerType, family, day);
                    }
                },
                writers.ProcessBehavior.SaveFilledDay);

            if (!percentilesAreDone || !limitsAreDone)
            {
                // Deliberately not remembered: a day lost to a fault is worth another try, unlike one
                // the walk declined on the merits.
                return;
            }

            // Worked out once is worked out for good: the reading follows from the owner's stored
            // items, and those change only when the owner is refreshed - which is the event that
            // forgets this again. The day the walk declined to write is the case this exists for,
            // because left unremembered it is found missing by every later chart load, each of
            // which starts another pass that declines it again.
            memo.TheWalkHasAlreadyWorkedOut(request.OwnerId, request.OwnerType, day);
        }

        private async Task<bool> TryWriteDayAsync(
            OverTimeFillRequest request, DateOnly day, Action stageEveryFamily, Func<Task> commit)
        {
            try
            {
                stageEveryFamily();
                await commit();

                return true;
            }
            catch (Exception failure)
            {
                logger.LogError(
                    failure,
                    "Over-time reconstruction could not write {Day} for {OwnerType} {OwnerId} ({MetricFamily}); the rest of the pass continues",
                    day,
                    request.OwnerType,
                    request.OwnerId,
                    MetricFamily);

                return false;
            }
        }

        private void Forget((int OwnerId, OwnerType OwnerType) key)
        {
            lock (alreadyAsked)
            {
                alreadyAsked.Remove(key);
            }
        }

        /// <summary>
        /// The two write policies a pass commits through. Held together because a day is one day
        /// across both of them: the pass writes each family's row and only then counts the day done.
        /// </summary>
        private sealed record OverTimeWriters(
            IPercentileSnapshotWriter Percentiles,
            IProcessBehaviorSnapshotWriter ProcessBehavior);
    }
}
