using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    public class ProcessBehaviorRecordingHandler
        : IDomainEventHandler<TeamDataRefreshed>,
          IDomainEventHandler<PortfolioFeaturesRefreshed>
    {
        private const string MetricFamily = "ProcessBehavior";

        // PortfolioMetricsView hard-codes defaultDateRange={90}.
        private const int PortfolioLookbackDays = 90;

        // TeamMetricsView falls back to a 30-day range when the team pins fixed throughput dates,
        // because a fixed past window is not an as-of-today window.
        private const int FixedDatesTeamLookbackDays = 30;

        private readonly ITeamMetricsService teamMetricsService;
        private readonly IPortfolioMetricsService portfolioMetricsService;
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Portfolio> portfolioRepository;
        private readonly IProcessBehaviorSnapshotRepository snapshotRepository;
        private readonly ILogger<ProcessBehaviorRecordingHandler> logger;

        public ProcessBehaviorRecordingHandler(
            ITeamMetricsService teamMetricsService,
            IPortfolioMetricsService portfolioMetricsService,
            IRepository<Team> teamRepository,
            IRepository<Portfolio> portfolioRepository,
            IProcessBehaviorSnapshotRepository snapshotRepository,
            ILogger<ProcessBehaviorRecordingHandler> logger)
        {
            this.teamMetricsService = teamMetricsService;
            this.portfolioMetricsService = portfolioMetricsService;
            this.teamRepository = teamRepository;
            this.portfolioRepository = portfolioRepository;
            this.snapshotRepository = snapshotRepository;
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
                LookbackDaysFor(team),
                TeamReaders(team),
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
                PortfolioLookbackDays,
                PortfolioReaders(portfolio),
                () => portfolioMetricsService.InvalidatePortfolioMetrics(portfolio));
        }

        // Five families for a team, six for a portfolio. The asymmetry is structural, not a filter:
        // Feature Size is a portfolio concept (D8) and there is no team-side read method to call.
        // Dropping a line here is a silent capability loss, so the recorder tests assert the exact
        // family SET each scope produces.
        private (ProcessBehaviorMetricType MetricType, Func<DateTime, DateTime, ProcessBehaviourChart> ReadChart)[] TeamReaders(Team team)
        {
            return
            [
                (ProcessBehaviorMetricType.Throughput, (startDate, endDate) => teamMetricsService.GetThroughputProcessBehaviourChart(team, startDate, endDate)),
                (ProcessBehaviorMetricType.WorkItemAge, (startDate, endDate) => teamMetricsService.GetTotalWorkItemAgeProcessBehaviourChart(team, startDate, endDate)),
                (ProcessBehaviorMetricType.Wip, (startDate, endDate) => teamMetricsService.GetWipProcessBehaviourChart(team, startDate, endDate)),
                (ProcessBehaviorMetricType.CycleTime, (startDate, endDate) => teamMetricsService.GetCycleTimeProcessBehaviourChart(team, startDate, endDate)),
                (ProcessBehaviorMetricType.Arrivals, (startDate, endDate) => teamMetricsService.GetArrivalsProcessBehaviourChart(team, startDate, endDate)),
            ];
        }

        private (ProcessBehaviorMetricType MetricType, Func<DateTime, DateTime, ProcessBehaviourChart> ReadChart)[] PortfolioReaders(Portfolio portfolio)
        {
            return
            [
                (ProcessBehaviorMetricType.Throughput, (startDate, endDate) => portfolioMetricsService.GetThroughputProcessBehaviourChart(portfolio, startDate, endDate)),
                (ProcessBehaviorMetricType.WorkItemAge, (startDate, endDate) => portfolioMetricsService.GetTotalWorkItemAgeProcessBehaviourChart(portfolio, startDate, endDate)),
                (ProcessBehaviorMetricType.Wip, (startDate, endDate) => portfolioMetricsService.GetWipProcessBehaviourChart(portfolio, startDate, endDate)),
                (ProcessBehaviorMetricType.CycleTime, (startDate, endDate) => portfolioMetricsService.GetCycleTimeProcessBehaviourChart(portfolio, startDate, endDate)),
                (ProcessBehaviorMetricType.Arrivals, (startDate, endDate) => portfolioMetricsService.GetArrivalsProcessBehaviourChart(portfolio, startDate, endDate)),
                (ProcessBehaviorMetricType.FeatureSize, (startDate, endDate) => portfolioMetricsService.GetFeatureSizeProcessBehaviourChart(portfolio, startDate, endDate)),
            ];
        }

        // The day grain is an as-of-today window that mirrors the point-in-time throughputPbc widget,
        // so the recorded triple equals what the user sees today: BaseMetricsView asks for
        // [today - defaultDateRange, today], and TeamMetricsView derives that range from the span of
        // the team's own throughput history window (its fixed-dates branch falls back to 30 days).
        private static int LookbackDaysFor(Team team)
        {
            if (team.UseFixedDatesForThroughput)
            {
                return FixedDatesTeamLookbackDays;
            }

            var throughputSettings = team.GetThroughputSettings();
            return (int)(throughputSettings.EndDate - throughputSettings.StartDate).TotalDays;
        }

        private async Task RecordAsync(
            int ownerId,
            OwnerType ownerType,
            int lookbackDays,
            (ProcessBehaviorMetricType MetricType, Func<DateTime, DateTime, ProcessBehaviourChart> ReadChart)[] readers,
            Action invalidateReadCache)
        {
            try
            {
                var endDate = DateTime.Today;
                var startDate = endDate.AddDays(-lookbackDays);

                foreach (var reader in readers)
                {
                    RecordMetricType(ownerId, ownerType, reader.MetricType, startDate, endDate, reader.ReadChart);
                }

                await snapshotRepository.Save();
            }
            catch (Exception exception)
            {
                LogRecordingFailure(exception, ownerType, ownerId);
            }
            finally
            {
                // Reading the point-in-time chart above warms the shared metrics cache under the same
                // (owner, window) key the widget reads. Recording runs on the refresh event, which can
                // fire on partially-seeded data, so leaving that entry behind would serve the UI a stale
                // snapshot instead of a value computed on the settled data.
                invalidateReadCache();
            }
        }

        private void RecordMetricType(
            int ownerId,
            OwnerType ownerType,
            ProcessBehaviorMetricType metricType,
            DateTime startDate,
            DateTime endDate,
            Func<DateTime, DateTime, ProcessBehaviourChart> readChart)
        {
            try
            {
                var chart = readChart(startDate, endDate);

                // Honesty gate: ProcessBehaviourChart.NotReady returns Average = UNPL = LNPL = 0.
                // Persisting that triple would draw three flat lines pinned at zero — a process the
                // owner never had. An absent row is the honest empty state.
                if (chart.Status != BaselineStatus.Ready)
                {
                    return;
                }

                // A Ready chart can still carry a fully collapsed band: XmRCalculator.Calculate returns
                // Average = UNPL = LNPL = 0 for an empty or all-zero baseline, and every chart builder
                // still stamps Status = Ready for it. Persisting that triple has the same effect as
                // persisting NotReady, so it is refused here too. LowerNaturalProcessLimit is
                // deliberately NOT part of the predicate — the calculator clamps a negative lower limit
                // to zero for zero-bounded data, so a real, busy process routinely reports Lnpl == 0.
                if (chart.Average == 0 && chart.UpperNaturalProcessLimit == 0)
                {
                    return;
                }

                UpsertSnapshot(ownerId, ownerType, metricType, DateOnly.FromDateTime(endDate), chart);
            }
            catch (Exception exception)
            {
                LogRecordingFailure(exception, ownerType, ownerId);
            }
        }

        private void UpsertSnapshot(
            int ownerId,
            OwnerType ownerType,
            ProcessBehaviorMetricType metricType,
            DateOnly recordedAt,
            ProcessBehaviourChart chart)
        {
            var existing = snapshotRepository.GetByPredicate(
                s => s.OwnerId == ownerId &&
                     s.OwnerType == ownerType &&
                     s.MetricType == metricType &&
                     s.RecordedAt == recordedAt);

            if (existing != null)
            {
                existing.Unpl = chart.UpperNaturalProcessLimit;
                existing.Average = chart.Average;
                existing.Lnpl = chart.LowerNaturalProcessLimit;
            }
            else
            {
                snapshotRepository.Add(new ProcessBehaviorSnapshot
                {
                    OwnerId = ownerId,
                    OwnerType = ownerType,
                    MetricType = metricType,
                    RecordedAt = recordedAt,
                    Unpl = chart.UpperNaturalProcessLimit,
                    Average = chart.Average,
                    Lnpl = chart.LowerNaturalProcessLimit,
                });
            }
        }

        private void LogRecordingFailure(Exception exception, OwnerType ownerType, int ownerId)
        {
            logger.LogError(
                exception,
                "Process behaviour snapshot recording failed for {OwnerType} {OwnerId} ({MetricFamily})",
                ownerType,
                ownerId,
                MetricFamily);
        }
    }
}
