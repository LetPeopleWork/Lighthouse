using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Platform.Host;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application.Policies
{
    public sealed class WithinSleRecordingHandler(
        ITeamMetricsService teamMetricsService,
        IPortfolioMetricsService portfolioMetricsService,
        IRepository<Team> teamRepository,
        IRepository<Portfolio> portfolioRepository,
        IWithinSleSnapshotWriter snapshotWriter,
        ILighthouseClock clock,
        ILogger<WithinSleRecordingHandler> logger)
        : IDomainEventHandler<TeamDataRefreshed>,
          IDomainEventHandler<PortfolioFeaturesRefreshed>
    {
        // Operator alerting groups recording failures by metric family; this record is a family of its own.
        private const string MetricFamily = "WithinSle";

        public async Task HandleAsync(TeamDataRefreshed domainEvent, CancellationToken cancellationToken)
        {
            var team = teamRepository.GetById(domainEvent.TeamId);
            if (team == null)
            {
                return;
            }

            await RecordAsync(
                team.Id,
                OwnerType.Team,
                WithinSleFamilies.For(team, teamMetricsService, clock),
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
                portfolio.Id,
                OwnerType.Portfolio,
                WithinSleFamilies.For(portfolio, portfolioMetricsService, clock),
                () => portfolioMetricsService.InvalidatePortfolioMetrics(portfolio));
        }

        private async Task RecordAsync(int ownerId, OwnerType ownerType, WithinSleFamilyReader family, Action invalidateReadCache)
        {
            try
            {
                await snapshotWriter.RecordToday(ownerId, ownerType, family);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Within SLE snapshot recording failed for {OwnerType} {OwnerId} ({MetricFamily})",
                    ownerType,
                    ownerId,
                    MetricFamily);
            }
            finally
            {
                // Reading the windows warms the metrics cache under keys the widgets read too, and the refresh
                // can fire on data that is still settling; dropping what was warmed makes the widgets recompute.
                invalidateReadCache();
            }
        }
    }
}
