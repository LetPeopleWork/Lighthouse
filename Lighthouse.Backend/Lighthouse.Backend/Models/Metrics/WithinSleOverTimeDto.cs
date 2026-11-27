namespace Lighthouse.Backend.Models.Metrics
{
    public sealed record WithinSleOverTimeDto(
        int? CurrentSlePercentile,
        int? CurrentSleDays,
        IReadOnlyList<WithinSleDayDto> Days,
        int MinimumClosed);
}
