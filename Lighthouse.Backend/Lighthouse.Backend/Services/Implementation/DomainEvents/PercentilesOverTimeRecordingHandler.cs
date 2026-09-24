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
        // Operator alerting groups failures by metric FAMILY, not by metric type: cycle time and work
        // item age are both percentile readings, so every failure this handler reports carries
        // "Percentiles". The ProcessBehavior family ships its own recorder.
        private const string MetricFamily = "Percentiles";

        private readonly ITeamMetricsService teamMetricsService;
        private readonly IPortfolioMetricsService portfolioMetricsService;
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Portfolio> portfolioRepository;
        private readonly IPercentilesOverTimeSnapshotRepository snapshotRepository;
        private readonly IPercentileSnapshotWriter snapshotWriter;
        private readonly ILogger<PercentilesOverTimeRecordingHandler> logger;

        public PercentilesOverTimeRecordingHandler(
            ITeamMetricsService teamMetricsService,
            IPortfolioMetricsService portfolioMetricsService,
            IRepository<Team> teamRepository,
            IRepository<Portfolio> portfolioRepository,
            IPercentilesOverTimeSnapshotRepository snapshotRepository,
            IPercentileSnapshotWriter snapshotWriter,
            ILogger<PercentilesOverTimeRecordingHandler> logger)
        {
            this.teamMetricsService = teamMetricsService;
            this.portfolioMetricsService = portfolioMetricsService;
            this.teamRepository = teamRepository;
            this.portfolioRepository = portfolioRepository;
            this.snapshotRepository = snapshotRepository;
            this.snapshotWriter = snapshotWriter;
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
                // Both families share this one pass — a second recorder would double the refresh cost
                // and drift from the cycle-time rows it is meant to sit beside.
                RecordFamily(ownerId, ownerType, MetricType.CycleTime, readCycleTimePercentiles);
                RecordFamily(ownerId, ownerType, MetricType.WorkItemAge, readWorkItemAgePercentiles);

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
            Func<DateTime, DateTime, IEnumerable<PercentileValue>> readPercentiles)
        {
            try
            {
                snapshotWriter.RecordToday(ownerId, ownerType, metricType, readPercentiles);
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
    }
}
