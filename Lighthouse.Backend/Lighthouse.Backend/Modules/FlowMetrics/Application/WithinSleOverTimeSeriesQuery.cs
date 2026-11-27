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
            var days = snapshots.GetSeries(ownerId, ownerType, horizon, from, to).Select(DayOf).ToList();

            return new WithinSleOverTimeDto(currentSle?.Percentile, currentSle?.Days, days, WithinSleShare.MinimumClosed);
        }

        private static WithinSleDayDto DayOf(WithinSleSnapshot snapshot)
        {
            var sle = ServiceLevelExpectation.Of(snapshot.SlePercentile, snapshot.SleDays)
                ?? throw new InvalidOperationException(
                    $"The Within SLE record of {snapshot.OwnerType} {snapshot.OwnerId} on {snapshot.RecordedAt} holds the SLE {snapshot.SlePercentile} % within {snapshot.SleDays} days, " +
                    "which is not a valid SLE. Only the Within SLE snapshot writer stores rows, and it stores none without a valid SLE: delete the row so the day can be filled in again.");

            return WithinSleShare.Counted(snapshot.WithinCount, snapshot.ClosedCount, sle) switch
            {
                WithinSleShare.Measured measured => DayOf(snapshot, WithinSleVerdict.Measured, measured.SharePercent),
                _ => DayOf(snapshot, WithinSleVerdict.TooFew, null),
            };
        }

        private static WithinSleDayDto DayOf(WithinSleSnapshot snapshot, WithinSleVerdict verdict, double? sharePercent)
            => new(snapshot.RecordedAt, snapshot.Horizon, snapshot.ClosedCount, snapshot.WithinCount, snapshot.SlePercentile, snapshot.SleDays, snapshot.Origin, verdict, sharePercent);

        private ServiceLevelExpectation? CurrentSleOf(int ownerId, OwnerType ownerType)
        {
            WorkTrackingSystemOptionsOwner? owner = ownerType == OwnerType.Team ? teams.GetById(ownerId) : portfolios.GetById(ownerId);

            return owner is null ? null : ServiceLevelExpectation.For(owner);
        }
    }
}
