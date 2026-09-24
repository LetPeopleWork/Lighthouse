using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    /// <summary>
    /// Everything one pass needs about one owner: how far forward its observations reach, every
    /// chart it has, and how to find where its stored items begin.
    ///
    /// Every chart rather than the one that was opened, because two asks for the same owner are
    /// the same ask. Narrow this to one family and the other families' asks are dropped while the
    /// work they cover is still outstanding, and the charts they belong to never fill at all.
    ///
    /// Both family lists are the ones the live recording uses rather than a copy made here. A copy
    /// is a second place the set can be changed, and the two then disagree about what a scope
    /// records without anything saying so. Each family is only wrapped, so that a reading reaching
    /// back past the work the owner still keeps comes back empty.
    /// </summary>
    internal sealed record OverTimeFillTarget(
        DateOnly LastObservedOn,
        IReadOnlyList<PercentileFamilyReader> PercentileFamilies,
        IReadOnlyList<ProcessBehaviorFamilyReader> ProcessBehaviorFamilies,
        Action InvalidateReadCache,
        Func<DateOnly?> EarliestDayTheItemsSupport)
    {
        /// <summary>
        /// The owner the ask names, loaded from the pass's own scope, or null when it no longer exists.
        /// </summary>
        public static OverTimeFillTarget? For(IServiceProvider services, OverTimeFillRequest request)
        {
            return request.OwnerType switch
            {
                OwnerType.Team => ForTeam(services, request.OwnerId),
                OwnerType.Portfolio => ForPortfolio(services, request.OwnerId),
                _ => null,
            };
        }

        private static OverTimeFillTarget? ForTeam(IServiceProvider services, int teamId)
        {
            var team = services.GetRequiredService<IRepository<Team>>().GetById(teamId);
            if (team is null)
            {
                return null;
            }

            var metrics = services.GetRequiredService<ITeamMetricsService>();
            var workItems = services.GetRequiredService<IWorkItemRepository>();
            var clock = services.GetRequiredService<ILighthouseClock>();
            var lastObservedOn = clock.ToInstanceDay(team.UpdateTime);
            var retention = RetentionEdge.For(lastObservedOn, team);

            return new OverTimeFillTarget(
                lastObservedOn,
                [.. Implementation.PercentileFamilies.For(team, metrics).Select(retention.Guard)],
                [.. services.GetRequiredService<IProcessBehaviorSnapshotWriter>().FamiliesFor(team).Select(retention.Guard)],
                () => metrics.InvalidateTeamMetrics(team),
                () => retention.NoEarlierThanTheEdge(EarliestFinishedDay(
                    clock,
                    workItems.GetAllByPredicate(item => item.TeamId == teamId && item.ClosedDate != null)
                        .Select(item => item.ClosedDate))));
        }

        private static OverTimeFillTarget? ForPortfolio(IServiceProvider services, int portfolioId)
        {
            var portfolio = services.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId);
            if (portfolio is null)
            {
                return null;
            }

            var metrics = services.GetRequiredService<IPortfolioMetricsService>();
            var deliveries = services.GetRequiredService<IRepository<Feature>>();
            var clock = services.GetRequiredService<ILighthouseClock>();
            var lastObservedOn = clock.ToInstanceDay(portfolio.UpdateTime);
            var retention = RetentionEdge.For(lastObservedOn, portfolio);

            return new OverTimeFillTarget(
                lastObservedOn,
                [.. Implementation.PercentileFamilies.For(portfolio, metrics).Select(retention.Guard)],
                [.. services.GetRequiredService<IProcessBehaviorSnapshotWriter>().FamiliesFor(portfolio).Select(retention.Guard)],
                () => metrics.InvalidatePortfolioMetrics(portfolio),
                () => retention.NoEarlierThanTheEdge(EarliestFinishedDay(
                    clock,
                    deliveries
                        .GetAllByPredicate(delivery =>
                            delivery.ClosedDate != null && delivery.Portfolios.Any(owner => owner.Id == portfolioId))
                        .Select(delivery => delivery.ClosedDate))));
        }

        /// <summary>
        /// The first day the owner's stored items can support a reading, or null when nothing has ever
        /// finished. Asked of the database as one aggregate rather than by loading the items, because
        /// an owner with a long history has a lot of them and only the earliest one is wanted.
        /// </summary>
        private static DateOnly? EarliestFinishedDay(ILighthouseClock clock, IQueryable<DateTime?> finishedInstants)
        {
            var earliest = finishedInstants.Min();

            return earliest is null ? null : clock.ToInstanceDay(earliest.Value);
        }
    }
}
