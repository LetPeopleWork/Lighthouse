using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Modules.FlowMetrics.Domain;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    public sealed class WithinSleOverTimeSeriesQuery(IWithinSleSnapshotRepository snapshots, IRepository<Team> teams, IRepository<Portfolio> portfolios) : IWithinSleOverTimeSeriesQuery
    {
        public WithinSleOverTimeDto GetSeries(int ownerId, OwnerType ownerType, int horizon, DateOnly? from, DateOnly? to)
        {
            var currentSle = CurrentSleOf(ownerId, ownerType);
            var days = snapshots.GetSeries(ownerId, ownerType, horizon, from, to).Select(WithinSleDayDto.From).ToList();

            return new WithinSleOverTimeDto(currentSle?.Percentile, currentSle?.Days, days, WithinSleShare.MinimumClosed);
        }

        private ServiceLevelExpectation? CurrentSleOf(int ownerId, OwnerType ownerType)
        {
            WorkTrackingSystemOptionsOwner? owner = ownerType == OwnerType.Team ? teams.GetById(ownerId) : portfolios.GetById(ownerId);

            return owner is null ? null : ServiceLevelExpectation.For(owner);
        }
    }
}
