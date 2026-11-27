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
            throw new NotImplementedException("Reading a day of the Within SLE record is not built yet.");
        }
    }
}
