using System.Threading.Channels;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
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

        private readonly Channel<OverTimeFillRequest> waiting = Channel.CreateBounded<OverTimeFillRequest>(
            new BoundedChannelOptions(MostOwnersWaitingAtOnce) { FullMode = BoundedChannelFullMode.DropWrite });

        private readonly HashSet<(int OwnerId, OwnerType OwnerType, MetricType MetricType)> alreadyAsked = [];

        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<OverTimeHistoryFiller> logger;

        private int passesRunning;

        public OverTimeHistoryFiller(IServiceScopeFactory scopeFactory, ILogger<OverTimeHistoryFiller> logger)
        {
            this.scopeFactory = scopeFactory;
            this.logger = logger;
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

        private static async Task FillAsync(IServiceProvider services, OverTimeFillRequest request, CancellationToken cancellationToken)
        {
            var target = TargetFor(services, request);
            if (target is null)
            {
                return;
            }

            var writer = services.GetRequiredService<IPercentileSnapshotWriter>();

            foreach (var day in request.CandidateDays)
            {
                // Past the owner's last observation its items are frozen at the break, so a reading
                // there would draw a confident line over a period in which nothing was watched.
                if (day > target.LastObservedOn || cancellationToken.IsCancellationRequested)
                {
                    continue;
                }

                writer.FillDayIfAbsent(request.OwnerId, request.OwnerType, request.MetricType, day, target.ReadPercentiles);
            }

            await services.GetRequiredService<IPercentilesOverTimeSnapshotRepository>().Save();

            // The readings above warmed the shared metrics cache under the same (owner, window) keys
            // the widgets read. Leaving them behind would serve the UI values computed for a day that
            // is not the one it is asking about.
            target.InvalidateReadCache();
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

            Func<DateTime, DateTime, IEnumerable<PercentileValue>> readPercentiles =
                metricType == MetricType.WorkItemAge
                    ? (_, windowEnd) => metrics.GetWorkItemAgePercentilesForTeam(team, windowEnd)
                    : (windowStart, windowEnd) => metrics.GetCycleTimePercentilesForTeam(team, windowStart, windowEnd);

            return new PassTarget(
                DateOnly.FromDateTime(team.UpdateTime),
                readPercentiles,
                () => metrics.InvalidateTeamMetrics(team));
        }

        private static PassTarget? PortfolioTarget(IServiceProvider services, int portfolioId, MetricType metricType)
        {
            var portfolio = services.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId);
            if (portfolio is null)
            {
                return null;
            }

            var metrics = services.GetRequiredService<IPortfolioMetricsService>();

            Func<DateTime, DateTime, IEnumerable<PercentileValue>> readPercentiles =
                metricType == MetricType.WorkItemAge
                    ? (_, windowEnd) => metrics.GetWorkItemAgePercentilesForPortfolio(portfolio, windowEnd)
                    : (windowStart, windowEnd) => metrics.GetCycleTimePercentilesForPortfolio(portfolio, windowStart, windowEnd);

            return new PassTarget(
                DateOnly.FromDateTime(portfolio.UpdateTime),
                readPercentiles,
                () => metrics.InvalidatePortfolioMetrics(portfolio));
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
            Action InvalidateReadCache);
    }
}
