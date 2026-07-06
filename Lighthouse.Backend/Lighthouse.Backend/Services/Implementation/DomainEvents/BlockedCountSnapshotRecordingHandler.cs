using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    public class BlockedCountSnapshotRecordingHandler
        : IDomainEventHandler<TeamDataRefreshed>,
          IDomainEventHandler<PortfolioFeaturesRefreshed>
    {
        private readonly IWorkItemRepository workItemRepository;
        private readonly IRepository<Feature> featureRepository;
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Portfolio> portfolioRepository;
        private readonly IBlockedItemService blockedItemService;
        private readonly IBlockedCountSnapshotRepository snapshotRepository;
        private readonly ILogger<BlockedCountSnapshotRecordingHandler> logger;

        public BlockedCountSnapshotRecordingHandler(
            IWorkItemRepository workItemRepository,
            IRepository<Feature> featureRepository,
            IRepository<Team> teamRepository,
            IRepository<Portfolio> portfolioRepository,
            IBlockedItemService blockedItemService,
            IBlockedCountSnapshotRepository snapshotRepository,
            ILogger<BlockedCountSnapshotRecordingHandler> logger)
        {
            this.workItemRepository = workItemRepository;
            this.featureRepository = featureRepository;
            this.teamRepository = teamRepository;
            this.portfolioRepository = portfolioRepository;
            this.blockedItemService = blockedItemService;
            this.snapshotRepository = snapshotRepository;
            this.logger = logger;
        }

        public Task HandleAsync(TeamDataRefreshed domainEvent, CancellationToken cancellationToken)
        {
            logger.LogDebug("Recording blocked-item snapshot for Team {TeamId}", domainEvent.TeamId);

            var team = teamRepository.GetById(domainEvent.TeamId);
            if (team == null)
            {
                return Task.CompletedTask;
            }

            var workItems = workItemRepository
                .GetAllByPredicate(w => w.TeamId == domainEvent.TeamId)
                .ToList();

            var blockedCount = workItems.Count(item => blockedItemService.IsBlocked(item, team));

            UpsertSnapshot(domainEvent.TeamId, OwnerType.Team, blockedCount);

            return Task.CompletedTask;
        }

        public Task HandleAsync(PortfolioFeaturesRefreshed domainEvent, CancellationToken cancellationToken)
        {
            logger.LogDebug("Recording blocked-item snapshot for Portfolio {PortfolioId}", domainEvent.PortfolioId);

            var portfolio = portfolioRepository.GetById(domainEvent.PortfolioId);
            if (portfolio == null)
            {
                return Task.CompletedTask;
            }

            var features = featureRepository
                .GetAllByPredicate(f => f.Portfolios.Any(p => p.Id == domainEvent.PortfolioId))
                .ToList();

            var blockedCount = features.Count(item => blockedItemService.IsBlocked(item, portfolio));

            UpsertSnapshot(domainEvent.PortfolioId, OwnerType.Portfolio, blockedCount);

            return Task.CompletedTask;
        }

        private void UpsertSnapshot(int ownerId, OwnerType ownerType, int blockedCount)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var existing = snapshotRepository.GetByPredicate(
                s => s.OwnerId == ownerId && s.OwnerType == ownerType && s.RecordedAt == today);

            if (existing != null)
            {
                existing.BlockedCount = blockedCount;
                snapshotRepository.Update(existing);
            }
            else
            {
                var snapshot = new BlockedCountSnapshot
                {
                    OwnerId = ownerId,
                    OwnerType = ownerType,
                    RecordedAt = today,
                    BlockedCount = blockedCount,
                };
                snapshotRepository.Add(snapshot);
            }

            snapshotRepository.Save();
        }
    }
}
