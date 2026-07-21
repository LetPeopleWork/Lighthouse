using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    /// <summary>
    /// Demo-only: after a demo owner refreshes, synthesizes a backdated blocked history so the
    /// Blocked Over Time chart shows a real trend and each bar drills into the items blocked at that
    /// date. The per-sync <see cref="BlockedCountSnapshotRecordingHandler"/> only ever records
    /// "today" and <see cref="WorkItemBlockedTransitionCaptureHandler"/> stamps EnteredAt = now, so a
    /// freshly-loaded demo has no history to visualise. This handler backfills that once.
    ///
    /// Gated to demo connections (SynthesizeStateJourneyForDemo = true) and idempotent (skips once a
    /// backdated snapshot exists), so it never touches a real customer's blocked data.
    /// </summary>
    public class DemoBlockedHistoryBackfillHandler
        : IDomainEventHandler<TeamDataRefreshed>,
          IDomainEventHandler<PortfolioFeaturesRefreshed>
    {
        private const int HistoryWindowDays = 14;

        private readonly ITeamMetricsService teamMetricsService;
        private readonly IPortfolioMetricsService portfolioMetricsService;
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Portfolio> portfolioRepository;
        private readonly IRepository<WorkTrackingSystemConnection> connectionRepository;
        private readonly IBlockedItemService blockedItemService;
        private readonly IWorkItemBlockedTransitionRepository transitionRepository;
        private readonly IBlockedCountSnapshotRepository snapshotRepository;
        private readonly ILogger<DemoBlockedHistoryBackfillHandler> logger;

#pragma warning disable S107 // This demo backfill genuinely needs both metrics services, both owner repos, the connection repo (demo gate), the blocked-item service and both the transition + snapshot repos; grouping them into an aggregate purely to dodge the 7-param threshold would add indirection without a domain rationale (same rationale as BlockedCountSnapshotRecordingHandler + TeamMetricsController).
        public DemoBlockedHistoryBackfillHandler(
            ITeamMetricsService teamMetricsService,
            IPortfolioMetricsService portfolioMetricsService,
            IRepository<Team> teamRepository,
            IRepository<Portfolio> portfolioRepository,
            IRepository<WorkTrackingSystemConnection> connectionRepository,
            IBlockedItemService blockedItemService,
            IWorkItemBlockedTransitionRepository transitionRepository,
            IBlockedCountSnapshotRepository snapshotRepository,
            ILogger<DemoBlockedHistoryBackfillHandler> logger)
#pragma warning restore S107
        {
            this.teamMetricsService = teamMetricsService;
            this.portfolioMetricsService = portfolioMetricsService;
            this.teamRepository = teamRepository;
            this.portfolioRepository = portfolioRepository;
            this.connectionRepository = connectionRepository;
            this.blockedItemService = blockedItemService;
            this.transitionRepository = transitionRepository;
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

            var blockedItems = teamMetricsService.GetBlockedEligibleItemsForTeam(team)
                .Where(item => blockedItemService.IsBlocked(item, team))
                .OrderBy(item => item.Id)
                .Select(item => (item.Id, item.StartedDate))
                .ToList();

            await BackfillAsync(team.Id, OwnerType.Team, blockedItems);
        }

        public async Task HandleAsync(PortfolioFeaturesRefreshed domainEvent, CancellationToken cancellationToken)
        {
            var portfolio = portfolioRepository.GetById(domainEvent.PortfolioId);
            if (portfolio == null || !IsDemoOwner(portfolio))
            {
                return;
            }

            var blockedFeatures = portfolioMetricsService.GetBlockedEligibleFeaturesForPortfolio(portfolio)
                .Where(feature => blockedItemService.IsBlocked(feature, portfolio))
                .OrderBy(feature => feature.Id)
                .Select(feature => (feature.Id, feature.StartedDate))
                .ToList();

            await BackfillAsync(portfolio.Id, OwnerType.Portfolio, blockedFeatures);
        }

        private bool IsDemoOwner(WorkTrackingSystemOptionsOwner owner)
        {
            var connection = connectionRepository.GetById(owner.WorkTrackingSystemConnectionId);

            return connection != null
                && connection.Options.Any(option =>
                    option.Key == CsvWorkTrackingOptionNames.SynthesizeStateJourneyForDemo
                    && bool.TryParse(option.Value, out var synthesize) && synthesize);
        }

        private async Task BackfillAsync(int ownerId, OwnerType ownerType, List<(int WorkItemId, DateTime? StartedDate)> blockedItems)
        {
            if (blockedItems.Count == 0)
            {
                return;
            }

            var today = DateTime.Today;
            var todayDate = DateOnly.FromDateTime(today);

            // Idempotency: a backdated snapshot means this demo owner was already backfilled.
            // GetAllByPredicate (not GetByPredicate/Exists, which both use SingleOrDefault) because
            // the guard predicate matches the whole history window, not a single row.
            var alreadyBackfilled = snapshotRepository
                .GetAllByPredicate(snapshot =>
                    snapshot.OwnerId == ownerId
                    && snapshot.OwnerType == ownerType
                    && snapshot.RecordedAt < todayDate)
                .Any();

            if (alreadyBackfilled)
            {
                return;
            }

            // Stryker disable once all: diagnostic log text is not behaviour
            logger.LogInformation(
                "Backfilling demo blocked history for {OwnerType} {OwnerId} ({Count} blocked items)",
                ownerType, ownerId, blockedItems.Count);

            var entered = SpreadEnteredDates(blockedItems, today);

            // Blocked spells live in the team keyspace only. The portfolio path's item ids are
            // Feature.Ids, which collide with real WorkItem ids and corrupt the team historic blocked
            // read (a WorkItemBlockedTransition keyed by a Feature.Id makes a never-blocked work item
            // read blocked). So only the team path writes WorkItemBlockedTransition rows; the portfolio
            // path keeps its snapshot backfill and its feature spells are (re)introduced into the
            // dedicated FeatureBlockedTransition keyspace by a later slice (ADR-102, US-01).
            if (ownerType == OwnerType.Team)
            {
                foreach (var (workItemId, enteredAt) in entered)
                {
                    UpsertBackdatedTransition(workItemId, enteredAt);
                }

                await transitionRepository.Save();
            }

            var earliest = entered.Min(e => e.EnteredAt).Date;
            for (var day = earliest; day <= today; day = day.AddDays(1))
            {
                var blockedCount = entered.Count(e => e.EnteredAt.Date <= day);
                UpsertSnapshot(ownerId, ownerType, DateOnly.FromDateTime(day), blockedCount);
            }

            await snapshotRepository.Save();
        }

        private static List<(int WorkItemId, DateTime EnteredAt)> SpreadEnteredDates(
            List<(int WorkItemId, DateTime? StartedDate)> blockedItems, DateTime today)
        {
            var count = blockedItems.Count;

            return blockedItems
                .Select((item, index) =>
                {
                    // Spread blocked-since across the window so the count climbs over time instead of
                    // stepping from 0 to N on a single day. Earliest item blocked longest.
                    var daysAgo = HistoryWindowDays - (int)Math.Round((double)index / count * HistoryWindowDays);
                    var enteredAt = today.AddDays(-daysAgo);

                    // Never claim an item was blocked before it was even started.
                    var startedCap = item.StartedDate?.Date;
                    if (startedCap.HasValue && enteredAt < startedCap.Value)
                    {
                        enteredAt = startedCap.Value;
                    }

                    return (item.WorkItemId, EnteredAt: enteredAt);
                })
                .ToList();
        }

        private void UpsertBackdatedTransition(int workItemId, DateTime enteredAt)
        {
            var existingOpen = transitionRepository.GetByPredicate(
                transition => transition.WorkItemId == workItemId && transition.LeftAt == null);

            if (existingOpen != null)
            {
                existingOpen.EnteredAt = enteredAt;
                return;
            }

            transitionRepository.Add(new WorkItemBlockedTransition
            {
                WorkItemId = workItemId,
                EnteredAt = enteredAt,
                LeftAt = null,
            });
        }

        private void UpsertSnapshot(int ownerId, OwnerType ownerType, DateOnly recordedAt, int blockedCount)
        {
            var existing = snapshotRepository.GetByPredicate(snapshot =>
                snapshot.OwnerId == ownerId
                && snapshot.OwnerType == ownerType
                && snapshot.RecordedAt == recordedAt);

            if (existing != null)
            {
                existing.BlockedCount = blockedCount;
                return;
            }

            snapshotRepository.Add(new BlockedCountSnapshot
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                RecordedAt = recordedAt,
                BlockedCount = blockedCount,
            });
        }
    }
}
