using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Platform.Host;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application.Policies
{
#pragma warning disable CS9113 // Skeleton: the handler uses these collaborators once it is filled in.
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
#pragma warning restore CS9113
    {
        public Task HandleAsync(TeamDataRefreshed domainEvent, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Recording a Team's day in the Within SLE record is not built yet.");
        }

        public Task HandleAsync(PortfolioFeaturesRefreshed domainEvent, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Recording a Portfolio's day in the Within SLE record is not built yet.");
        }
    }
}
