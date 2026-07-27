using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    public class PercentilesOverTimeRecordingHandler
        : IDomainEventHandler<TeamDataRefreshed>,
          IDomainEventHandler<PortfolioFeaturesRefreshed>
    {
        // The observability contract (ADR-107) keys operator alerting on the metric FAMILY, not the
        // metric type: cycle time and work item age are both percentile readings, so every failure this
        // handler reports carries "Percentiles". The ProcessBehavior family ships its own recorder.
        private const string MetricFamily = "Percentiles";

        private static readonly int[] CycleTimeHorizons = [30, 60, 90];

        // Work item age is measured as-of-today: it has no horizon dimension, so it runs the same
        // recording pass exactly once under the horizon-less sentinel.
        private static readonly int[] WorkItemAgeHorizons = [PercentilesOverTimeSnapshot.NoHorizon];

        private readonly ITeamMetricsService teamMetricsService;
        private readonly IPortfolioMetricsService portfolioMetricsService;
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Portfolio> portfolioRepository;
        private readonly IPercentilesOverTimeSnapshotRepository snapshotRepository;
        private readonly ILighthouseClock clock;
        private readonly ILogger<PercentilesOverTimeRecordingHandler> logger;

        public PercentilesOverTimeRecordingHandler(
            ITeamMetricsService teamMetricsService,
            IPortfolioMetricsService portfolioMetricsService,
            IRepository<Team> teamRepository,
            IRepository<Portfolio> portfolioRepository,
            IPercentilesOverTimeSnapshotRepository snapshotRepository,
            ILighthouseClock clock,
            ILogger<PercentilesOverTimeRecordingHandler> logger)
        {
            this.teamMetricsService = teamMetricsService;
            this.portfolioMetricsService = portfolioMetricsService;
            this.teamRepository = teamRepository;
            this.portfolioRepository = portfolioRepository;
            this.snapshotRepository = snapshotRepository;
            this.clock = clock;
            this.logger = logger;
        }

        public async Task HandleAsync(TeamDataRefreshed domainEvent, CancellationToken cancellationToken)
        {
            var team = teamRepository.GetById(domainEvent.TeamId);
            if (team == null)
            {
                return;
            }

            await RecordAsync(
                domainEvent.TeamId,
                OwnerType.Team,
                (startDate, endDate) => teamMetricsService.GetCycleTimePercentilesForTeam(team, startDate, endDate),
                (_, endDate) => teamMetricsService.GetWorkItemAgePercentilesForTeam(team, endDate),
                () => teamMetricsService.InvalidateTeamMetrics(team));
        }

        public async Task HandleAsync(PortfolioFeaturesRefreshed domainEvent, CancellationToken cancellationToken)
        {
            var portfolio = portfolioRepository.GetById(domainEvent.PortfolioId);
            if (portfolio == null)
            {
                return;
            }

            await RecordAsync(
                domainEvent.PortfolioId,
                OwnerType.Portfolio,
                (startDate, endDate) => portfolioMetricsService.GetCycleTimePercentilesForPortfolio(portfolio, startDate, endDate),
                (_, endDate) => portfolioMetricsService.GetWorkItemAgePercentilesForPortfolio(portfolio, endDate),
                () => portfolioMetricsService.InvalidatePortfolioMetrics(portfolio));
        }

        private async Task RecordAsync(
            int ownerId,
            OwnerType ownerType,
            Func<DateTime, DateTime, IEnumerable<PercentileValue>> readCycleTimePercentiles,
            Func<DateTime, DateTime, IEnumerable<PercentileValue>> readWorkItemAgePercentiles,
            Action invalidateReadCache)
        {
            try
            {
                // Bug #5567: the range end is the instance calendar day, not the host zone's.
                var endDate = clock.TodayAsUtcMidnight;

                // Both families share this one pass — a second recorder would double the refresh cost
                // and drift from the cycle-time rows it is meant to sit beside.
                RecordFamily(ownerId, ownerType, MetricType.CycleTime, CycleTimeHorizons, endDate, readCycleTimePercentiles);
                RecordFamily(ownerId, ownerType, MetricType.WorkItemAge, WorkItemAgeHorizons, endDate, readWorkItemAgePercentiles);

                await snapshotRepository.Save();
            }
            catch (Exception exception)
            {
                LogRecordingFailure(exception, ownerType, ownerId);
            }
            finally
            {
                // Reading the point-in-time percentiles above warms the shared metrics cache under the
                // same (owner, window) keys the Flow Overview widgets read. Recording runs on the refresh
                // event, which can fire on partially-seeded data (e.g. demo load), so leaving those entries
                // behind would serve the UI a stale snapshot instead of a value computed on the settled data.
                // Invalidate what we warmed so the UI recomputes lazily — restoring the post-refresh
                // "cache empty, compute on first read" behaviour that predates this handler.
                invalidateReadCache();
            }
        }

        private void RecordFamily(
            int ownerId,
            OwnerType ownerType,
            MetricType metricType,
            int[] horizons,
            DateTime endDate,
            Func<DateTime, DateTime, IEnumerable<PercentileValue>> readPercentiles)
        {
            // Bug #5567: the day key comes from the seam rather than re-reducing the instant
            // argument - a derived reduction is the same defect one call deeper.
            var recordedAt = clock.Today;

            try
            {
                foreach (var horizon in horizons)
                {
                    var percentiles = readPercentiles(endDate.AddDays(-horizon), endDate).ToList();
                    UpsertSnapshot(ownerId, ownerType, metricType, horizon, recordedAt, percentiles);
                }
            }
            catch (Exception exception)
            {
                // Contained per family so a failing family never discards the rows the other one
                // already staged — the caller's single Save() still persists them.
                LogRecordingFailure(exception, ownerType, ownerId);
            }
        }

        private void LogRecordingFailure(Exception exception, OwnerType ownerType, int ownerId)
        {
            logger.LogError(
                exception,
                "Percentile snapshot recording failed for {OwnerType} {OwnerId} ({MetricFamily})",
                ownerType,
                ownerId,
                MetricFamily);
        }

        private void UpsertSnapshot(
            int ownerId,
            OwnerType ownerType,
            MetricType metricType,
            int horizon,
            DateOnly recordedAt,
            List<PercentileValue> percentiles)
        {
            var existing = snapshotRepository.GetByPredicate(
                s => s.OwnerId == ownerId &&
                     s.OwnerType == ownerType &&
                     s.MetricType == metricType &&
                     s.Horizon == horizon &&
                     s.RecordedAt == recordedAt);

            var p50 = ValueFor(percentiles, 50);
            var p70 = ValueFor(percentiles, 70);
            var p85 = ValueFor(percentiles, 85);
            var p95 = ValueFor(percentiles, 95);

            if (existing != null)
            {
                existing.P50 = p50;
                existing.P70 = p70;
                existing.P85 = p85;
                existing.P95 = p95;
            }
            else
            {
                snapshotRepository.Add(new PercentilesOverTimeSnapshot
                {
                    OwnerId = ownerId,
                    OwnerType = ownerType,
                    MetricType = metricType,
                    Horizon = horizon,
                    RecordedAt = recordedAt,
                    P50 = p50,
                    P70 = p70,
                    P85 = p85,
                    P95 = p95,
                });
            }
        }

        private static int ValueFor(List<PercentileValue> percentiles, int percentile)
        {
            return percentiles.FirstOrDefault(p => p.Percentile == percentile)?.Value ?? 0;
        }
    }
}
