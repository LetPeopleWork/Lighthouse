using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    /// <summary>
    /// Demo-only: after a demo owner refreshes, synthesizes a backdated cycle-time percentile history
    /// so the Percentiles Over Time chart renders a populated trend on a freshly-loaded demo. The
    /// per-sync <see cref="PercentilesOverTimeRecordingHandler"/> is strictly forward-only (records
    /// "today" only), so a fresh demo has no past days to plot. This handler backfills the window once.
    ///
    /// Cycle-time only (the sole metric in slice 01). Gated to demo connections
    /// (SynthesizeStateJourneyForDemo = true) and idempotent (skips once a backdated snapshot exists),
    /// so it never touches a real customer's data — real tenants stay forward-only (DDD-4).
    /// </summary>
    public class DemoPercentilesBackfillHandler
        : IDomainEventHandler<TeamDataRefreshed>,
          IDomainEventHandler<PortfolioFeaturesRefreshed>
    {
        private const int HistoryWindowDays = 14;

        private static readonly int[] Horizons = [30, 60, 90];

        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Portfolio> portfolioRepository;
        private readonly IRepository<WorkTrackingSystemConnection> connectionRepository;
        private readonly IPercentilesOverTimeSnapshotRepository snapshotRepository;
        private readonly ILogger<DemoPercentilesBackfillHandler> logger;

        public DemoPercentilesBackfillHandler(
            IRepository<Team> teamRepository,
            IRepository<Portfolio> portfolioRepository,
            IRepository<WorkTrackingSystemConnection> connectionRepository,
            IPercentilesOverTimeSnapshotRepository snapshotRepository,
            ILogger<DemoPercentilesBackfillHandler> logger)
        {
            this.teamRepository = teamRepository;
            this.portfolioRepository = portfolioRepository;
            this.connectionRepository = connectionRepository;
            this.snapshotRepository = snapshotRepository;
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
            var todayDate = DateOnly.FromDateTime(DateTime.Today);

            // Idempotency: a backdated snapshot means this demo owner's history was already backfilled.
            // The forward-only recording handler only writes RecordedAt == today, so RecordedAt < today
            // is a clean "backfill already ran" signal that never collides with the live sync.
            var alreadyBackfilled = snapshotRepository
                .GetAllByPredicate(snapshot =>
                    snapshot.OwnerId == ownerId
                    && snapshot.OwnerType == ownerType
                    && snapshot.MetricType == MetricType.CycleTime
                    && snapshot.RecordedAt < todayDate)
                .Any();

            if (alreadyBackfilled)
            {
                return;
            }

            // Stryker disable once all: diagnostic log text is not behaviour
            logger.LogInformation(
                "Backfilling demo cycle-time percentile history for {OwnerType} {OwnerId}",
                ownerType, ownerId);

            for (var daysAgo = HistoryWindowDays; daysAgo >= 1; daysAgo--)
            {
                var recordedAt = todayDate.AddDays(-daysAgo);
                var dayIndex = HistoryWindowDays - daysAgo;

                foreach (var horizon in Horizons)
                {
                    var percentiles = SynthesizePercentiles(dayIndex, horizon);
                    UpsertSnapshot(ownerId, ownerType, horizon, recordedAt, percentiles);
                }
            }

            await snapshotRepository.Save();
        }

        private static (int P50, int P70, int P85, int P95) SynthesizePercentiles(int dayIndex, int horizon)
        {
            // Deterministic gentle wave so the demo chart shows a trend, not a flat line. The horizon
            // offset keeps the three lines visually distinct. Percentiles are monotone non-decreasing.
            var p50 = 4 + (dayIndex % 5) + horizon / 30;
            var p70 = p50 + 3;
            var p85 = p70 + 4;
            var p95 = p85 + 5;
            return (p50, p70, p85, p95);
        }

        private void UpsertSnapshot(
            int ownerId, OwnerType ownerType, int horizon, DateOnly recordedAt,
            (int P50, int P70, int P85, int P95) percentiles)
        {
            var existing = snapshotRepository.GetByPredicate(snapshot =>
                snapshot.OwnerId == ownerId
                && snapshot.OwnerType == ownerType
                && snapshot.MetricType == MetricType.CycleTime
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
                MetricType = MetricType.CycleTime,
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
