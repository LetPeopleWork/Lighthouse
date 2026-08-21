using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    /// <summary>
    /// Demo-only: after a demo owner refreshes, synthesizes a backdated percentile history so the
    /// Percentiles Over Time chart renders a populated trend on a freshly-loaded demo. The per-sync
    /// <see cref="PercentilesOverTimeRecordingHandler"/> is strictly forward-only (records "today"
    /// only), so a fresh demo has no past days to plot. This handler backfills the window once.
    ///
    /// Backdates BOTH over-time families, over one shared window so the widgets never disagree
    /// about their date range on a demo screenshot. Percentiles: cycle-time across the 30/60/90
    /// horizons, and work item age at the <see cref="PercentilesOverTimeSnapshot.NoHorizon"/>
    /// sentinel (age is always "as of today", so it has no horizon dimension). Process behaviour:
    /// the natural process limits of <see cref="ProcessBehaviorMetricType.Throughput"/>, which have
    /// no horizon dimension at all. Idempotency is evaluated PER FAMILY, so a demo owner that an
    /// earlier release backfilled with percentiles only still gains its throughput limits on the
    /// next refresh. Gated to demo connections (SynthesizeStateJourneyForDemo = true), so it never
    /// touches a real customer's data — real tenants stay forward-only (DDD-4).
    /// </summary>
    public class DemoPercentilesBackfillHandler
        : IDomainEventHandler<TeamDataRefreshed>,
          IDomainEventHandler<PortfolioFeaturesRefreshed>
    {
        private const int HistoryWindowDays = 14;

        private static readonly int[] CycleTimeHorizons = [30, 60, 90];

        private static readonly int[] WorkItemAgeHorizons = [PercentilesOverTimeSnapshot.NoHorizon];

        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Portfolio> portfolioRepository;
        private readonly IRepository<WorkTrackingSystemConnection> connectionRepository;
        private readonly IPercentilesOverTimeSnapshotRepository snapshotRepository;
        private readonly IProcessBehaviorSnapshotRepository processBehaviorSnapshotRepository;
        private readonly ILighthouseClock clock;
        private readonly ILogger<DemoPercentilesBackfillHandler> logger;

        public DemoPercentilesBackfillHandler(
            IRepository<Team> teamRepository,
            IRepository<Portfolio> portfolioRepository,
            IRepository<WorkTrackingSystemConnection> connectionRepository,
            IPercentilesOverTimeSnapshotRepository snapshotRepository,
            IProcessBehaviorSnapshotRepository processBehaviorSnapshotRepository,
            ILighthouseClock clock,
            ILogger<DemoPercentilesBackfillHandler> logger)
        {
            this.teamRepository = teamRepository;
            this.portfolioRepository = portfolioRepository;
            this.connectionRepository = connectionRepository;
            this.snapshotRepository = snapshotRepository;
            this.processBehaviorSnapshotRepository = processBehaviorSnapshotRepository;
            this.clock = clock;
            this.logger = logger;
        }

        public async Task HandleAsync(TeamDataRefreshed domainEvent, CancellationToken cancellationToken)
        {
            var team = teamRepository.GetById(domainEvent.TeamId);
            if (team == null || !IsDemoOwner(team))
            {
                return;
            }

            await BackfillAsync(team.Id, OwnerType.Team);
        }

        public async Task HandleAsync(PortfolioFeaturesRefreshed domainEvent, CancellationToken cancellationToken)
        {
            var portfolio = portfolioRepository.GetById(domainEvent.PortfolioId);
            if (portfolio == null || !IsDemoOwner(portfolio))
            {
                return;
            }

            await BackfillAsync(portfolio.Id, OwnerType.Portfolio);
        }

        private bool IsDemoOwner(WorkTrackingSystemOptionsOwner owner)
        {
            var connection = connectionRepository.GetById(owner.WorkTrackingSystemConnectionId);

            return connection != null
                && connection.Options.Any(option =>
                    option.Key == CsvWorkTrackingOptionNames.SynthesizeStateJourneyForDemo
                    && bool.TryParse(option.Value, out var synthesize) && synthesize);
        }

        private async Task BackfillAsync(int ownerId, OwnerType ownerType)
        {
            var todayDate = clock.Today;

            BackfillFamily(ownerId, ownerType, MetricType.CycleTime, CycleTimeHorizons, todayDate);
            BackfillFamily(ownerId, ownerType, MetricType.WorkItemAge, WorkItemAgeHorizons, todayDate);
            BackfillProcessBehaviorFamily(ownerId, ownerType, ProcessBehaviorMetricType.Throughput, todayDate);

            await snapshotRepository.Save();
            await processBehaviorSnapshotRepository.Save();
        }

        private void BackfillProcessBehaviorFamily(
            int ownerId, OwnerType ownerType, ProcessBehaviorMetricType metricType, DateOnly todayDate)
        {
            // Same per-family idempotency rule as the percentile families, evaluated against the
            // process-behaviour store: an owner an earlier release backfilled for percentiles only
            // must STILL gain its natural-process-limit history. An owner-scoped guard would make
            // every newly added family a permanent no-op wherever an older one already ran.
            var alreadyBackfilled = processBehaviorSnapshotRepository
                .GetAllByPredicate(snapshot =>
                    snapshot.OwnerId == ownerId
                    && snapshot.OwnerType == ownerType
                    && snapshot.MetricType == metricType
                    && snapshot.RecordedAt < todayDate)
                .Any();

            if (alreadyBackfilled)
            {
                return;
            }

            // Stryker disable once all: diagnostic log text is not behaviour
            logger.LogInformation(
                "Backfilling demo {MetricType} process behaviour history for {OwnerType} {OwnerId}",
                metricType, ownerType, ownerId);

            for (var daysAgo = HistoryWindowDays; daysAgo >= 1; daysAgo--)
            {
                var recordedAt = todayDate.AddDays(-daysAgo);
                var dayIndex = HistoryWindowDays - daysAgo;

                var limits = SynthesizeNaturalProcessLimits(dayIndex);
                UpsertProcessBehaviorSnapshot(ownerId, ownerType, metricType, recordedAt, limits);
            }
        }

        private static (int Lnpl, int Average, int Unpl) SynthesizeNaturalProcessLimits(int dayIndex)
        {
            // Same deterministic gentle wave as the percentile synthesis, so the two over-time
            // widgets on a demo screenshot move together. The limits are derived from the average by
            // a fixed spread, which makes LNPL < Average < UNPL structurally true for every day —
            // an inverted or degenerate (flat) triple would render a visibly broken chart.
            const int LimitSpread = 7;

            var average = 12 + (dayIndex % 5);
            return (average - LimitSpread, average, average + LimitSpread);
        }

        private void UpsertProcessBehaviorSnapshot(
            int ownerId, OwnerType ownerType, ProcessBehaviorMetricType metricType, DateOnly recordedAt,
            (int Lnpl, int Average, int Unpl) limits)
        {
            var existing = processBehaviorSnapshotRepository.GetByPredicate(snapshot =>
                snapshot.OwnerId == ownerId
                && snapshot.OwnerType == ownerType
                && snapshot.MetricType == metricType
                && snapshot.RecordedAt == recordedAt);

            if (existing != null)
            {
                existing.Lnpl = limits.Lnpl;
                existing.Average = limits.Average;
                existing.Unpl = limits.Unpl;
                return;
            }

            processBehaviorSnapshotRepository.Add(new ProcessBehaviorSnapshot
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                MetricType = metricType,
                RecordedAt = recordedAt,
                Lnpl = limits.Lnpl,
                Average = limits.Average,
                Unpl = limits.Unpl,
            });
        }

        private void BackfillFamily(
            int ownerId, OwnerType ownerType, MetricType metricType, int[] horizons, DateOnly todayDate)
        {
            // Idempotency is scoped to THIS metric family: a backdated snapshot of this family means
            // its history was already backfilled. Scoping matters — a shared "any family backdated"
            // check would make a newly-added family a permanent no-op wherever an older release
            // already backfilled a different one. The forward-only recording handler only writes
            // RecordedAt == today, so RecordedAt < today is a clean "backfill already ran" signal
            // that never collides with the live sync.
            var alreadyBackfilled = snapshotRepository
                .GetAllByPredicate(snapshot =>
                    snapshot.OwnerId == ownerId
                    && snapshot.OwnerType == ownerType
                    && snapshot.MetricType == metricType
                    && snapshot.RecordedAt < todayDate)
                .Any();

            if (alreadyBackfilled)
            {
                return;
            }

            // Stryker disable once all: diagnostic log text is not behaviour
            logger.LogInformation(
                "Backfilling demo {MetricType} percentile history for {OwnerType} {OwnerId}",
                metricType, ownerType, ownerId);

            for (var daysAgo = HistoryWindowDays; daysAgo >= 1; daysAgo--)
            {
                var recordedAt = todayDate.AddDays(-daysAgo);
                var dayIndex = HistoryWindowDays - daysAgo;

                foreach (var horizon in horizons)
                {
                    var percentiles = SynthesizePercentiles(dayIndex, horizon);
                    UpsertSnapshot(ownerId, ownerType, metricType, horizon, recordedAt, percentiles);
                }
            }
        }

        private static (int P50, int P70, int P85, int P95) SynthesizePercentiles(int dayIndex, int horizon)
        {
            // Deterministic gentle wave so the demo chart shows a trend, not a flat line. The horizon
            // offset keeps the three cycle-time lines visually distinct; work item age passes the
            // NoHorizon sentinel and so takes a zero offset. Percentiles are monotone non-decreasing.
            var p50 = 4 + (dayIndex % 5) + horizon / 30;
            var p70 = p50 + 3;
            var p85 = p70 + 4;
            var p95 = p85 + 5;
            return (p50, p70, p85, p95);
        }

        private void UpsertSnapshot(
            int ownerId, OwnerType ownerType, MetricType metricType, int horizon, DateOnly recordedAt,
            (int P50, int P70, int P85, int P95) percentiles)
        {
            var existing = snapshotRepository.GetByPredicate(snapshot =>
                snapshot.OwnerId == ownerId
                && snapshot.OwnerType == ownerType
                && snapshot.MetricType == metricType
                && snapshot.Horizon == horizon
                && snapshot.RecordedAt == recordedAt);

            if (existing != null)
            {
                existing.P50 = percentiles.P50;
                existing.P70 = percentiles.P70;
                existing.P85 = percentiles.P85;
                existing.P95 = percentiles.P95;
                return;
            }

            snapshotRepository.Add(new PercentilesOverTimeSnapshot
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                MetricType = metricType,
                Horizon = horizon,
                RecordedAt = recordedAt,
                P50 = percentiles.P50,
                P70 = percentiles.P70,
                P85 = percentiles.P85,
                P95 = percentiles.P95,
            });
        }
    }
}
