using Lighthouse.Backend.Modules.FlowMetrics.Domain;

namespace Lighthouse.Backend.Models.Metrics
{
    public sealed record WithinSleDayDto(
        DateOnly RecordedAt,
        int Horizon,
        int ClosedCount,
        int WithinCount,
        int SlePercentile,
        int SleDays,
        WithinSleSnapshotOrigin Origin,
        WithinSleVerdict Verdict,
        double? SharePercent)
    {
        public static WithinSleDayDto From(WithinSleSnapshot snapshot)
        {
            var sle = ServiceLevelExpectation.Of(snapshot.SlePercentile, snapshot.SleDays)
                ?? throw new InvalidOperationException(
                    $"The Within SLE record of {snapshot.OwnerType} {snapshot.OwnerId} on {snapshot.RecordedAt} holds the SLE {snapshot.SlePercentile} % within {snapshot.SleDays} days, " +
                    "which is not a valid SLE. Only the Within SLE snapshot writer stores rows, and it stores none without a valid SLE: delete the row so the day can be filled in again.");

            return WithinSleShare.Counted(snapshot.WithinCount, snapshot.ClosedCount, sle) switch
            {
                WithinSleShare.Measured measured => Of(snapshot, WithinSleVerdict.Measured, measured.SharePercent),
                _ => Of(snapshot, WithinSleVerdict.TooFew, null),
            };
        }

        private static WithinSleDayDto Of(WithinSleSnapshot snapshot, WithinSleVerdict verdict, double? sharePercent)
            => new(snapshot.RecordedAt, snapshot.Horizon, snapshot.ClosedCount, snapshot.WithinCount, snapshot.SlePercentile, snapshot.SleDays, snapshot.Origin, verdict, sharePercent);
    }
}
