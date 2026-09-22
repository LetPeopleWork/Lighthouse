using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
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

        private readonly ITeamMetricsService teamMetricsService;
        private readonly IPortfolioMetricsService portfolioMetricsService;
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Portfolio> portfolioRepository;
        private readonly IProcessBehaviorSnapshotRepository snapshotRepository;
        private readonly IProcessBehaviorSnapshotWriter snapshotWriter;
        private readonly ILogger<ProcessBehaviorRecordingHandler> logger;

        public ProcessBehaviorRecordingHandler(
            ITeamMetricsService teamMetricsService,
            IPortfolioMetricsService portfolioMetricsService,
            IRepository<Team> teamRepository,
            IRepository<Portfolio> portfolioRepository,
            IProcessBehaviorSnapshotRepository snapshotRepository,
            IProcessBehaviorSnapshotWriter snapshotWriter,
            ILogger<ProcessBehaviorRecordingHandler> logger)
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
                snapshotWriter.FamiliesFor(team),
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
                snapshotWriter.FamiliesFor(portfolio),
                () => portfolioMetricsService.InvalidatePortfolioMetrics(portfolio));
        }

        private async Task RecordAsync(
            int ownerId,
            OwnerType ownerType,
            IReadOnlyList<ProcessBehaviorFamilyReader> families,
            Action invalidateReadCache)
        {
            try
            {
                foreach (var family in families)
                {
                    RecordFamily(ownerId, ownerType, family);
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

        private void RecordFamily(int ownerId, OwnerType ownerType, ProcessBehaviorFamilyReader family)
        {
            try
            {
                snapshotWriter.RecordToday(ownerId, ownerType, family);
            }
            catch (Exception exception)
            {
                // Contained per family so a failing family never discards the rows its siblings
                // already staged — the caller's single Save() still persists them.
                LogRecordingFailure(exception, ownerType, ownerId);
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
