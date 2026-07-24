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
        private static readonly int[] Horizons = [30, 60, 90];

        private readonly ITeamMetricsService teamMetricsService;
        private readonly IPortfolioMetricsService portfolioMetricsService;
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Portfolio> portfolioRepository;
        private readonly IPercentilesOverTimeSnapshotRepository snapshotRepository;
        private readonly ILogger<PercentilesOverTimeRecordingHandler> logger;

        public PercentilesOverTimeRecordingHandler(
            ITeamMetricsService teamMetricsService,
            IPortfolioMetricsService portfolioMetricsService,
            IRepository<Team> teamRepository,
            IRepository<Portfolio> portfolioRepository,
            IPercentilesOverTimeSnapshotRepository snapshotRepository,
            ILogger<PercentilesOverTimeRecordingHandler> logger)
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
                (startDate, endDate) => teamMetricsService.GetCycleTimePercentilesForTeam(team, startDate, endDate));
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
                (startDate, endDate) => portfolioMetricsService.GetCycleTimePercentilesForPortfolio(portfolio, startDate, endDate));
        }

        private async Task RecordAsync(
            int ownerId,
            OwnerType ownerType,
            Func<DateTime, DateTime, IEnumerable<PercentileValue>> readPercentiles)
        {
            try
            {
                var endDate = DateTime.Today;
                var recordedAt = DateOnly.FromDateTime(endDate);

                foreach (var horizon in Horizons)
                {
                    var percentiles = readPercentiles(endDate.AddDays(-horizon), endDate).ToList();
                    UpsertSnapshot(ownerId, ownerType, horizon, recordedAt, percentiles);
                }

                await snapshotRepository.Save();
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Percentile snapshot recording failed for {OwnerType} {OwnerId} ({MetricFamily})",
                    ownerType,
                    ownerId,
                    "Percentiles");
            }
        }

        private void UpsertSnapshot(
            int ownerId,
            OwnerType ownerType,
            int horizon,
            DateOnly recordedAt,
            IReadOnlyList<PercentileValue> percentiles)
        {
            var existing = snapshotRepository.GetByPredicate(
                s => s.OwnerId == ownerId &&
                     s.OwnerType == ownerType &&
                     s.MetricType == MetricType.CycleTime &&
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
                    MetricType = MetricType.CycleTime,
                    Horizon = horizon,
                    RecordedAt = recordedAt,
                    P50 = p50,
                    P70 = p70,
                    P85 = p85,
                    P95 = p95,
                });
            }
        }

        private static int ValueFor(IReadOnlyList<PercentileValue> percentiles, int percentile)
        {
            return percentiles.FirstOrDefault(p => p.Percentile == percentile)?.Value ?? 0;
        }
    }
}
