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
        double? SharePercent);
}
